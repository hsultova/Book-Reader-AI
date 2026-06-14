using System.Text.Json;
using BookReaderApp.Configuration;
using Microsoft.Extensions.Options;

namespace BookReaderApp.Services;

// Calls the Google Books "volumes" REST endpoint from the backend. The API key is attached
// here server-side and never leaves the server. Parsing is defensive: any missing field or
// upstream failure degrades to an empty/partial result rather than throwing to the UI.
public class GoogleBooksService : IGoogleBooksService
{
    // Guards aligned with the Book model's column limits so prefilled values always validate.
    private const int MaxTitle = 200;
    private const int MaxIsbn = 20;
    private const int MaxDescription = 2000;
    private const int MaxResults = 40; // Google Books caps a single request at 40.

    private readonly HttpClient _http;
    private readonly GoogleBooksOptions _options;
    private readonly ILogger<GoogleBooksService> _logger;

    public GoogleBooksService(
        HttpClient http,
        IOptions<GoogleBooksOptions> options,
        ILogger<GoogleBooksService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GoogleBookResult>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<GoogleBookResult>();
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("Google Books search skipped: no API key configured.");
            return Array.Empty<GoogleBookResult>();
        }

        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var requestUri =
            $"{baseUrl}/volumes?q={Uri.EscapeDataString(query)}&maxResults={MaxResults}&key={Uri.EscapeDataString(_options.ApiKey)}";

        try
        {
            using var response = await _http.GetAsync(requestUri, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            return Parse(document);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            _logger.LogError(ex, "Google Books search failed for query '{Query}'.", query);
            return Array.Empty<GoogleBookResult>();
        }
    }

    private static IReadOnlyList<GoogleBookResult> Parse(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<GoogleBookResult>();
        }

        var results = new List<GoogleBookResult>();
        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("volumeInfo", out var info))
            {
                continue;
            }

            var title = GetString(info, "title");
            if (string.IsNullOrWhiteSpace(title))
            {
                continue; // Title is required on the Book model; skip unusable entries.
            }

            results.Add(new GoogleBookResult(
                Title: Truncate(title, MaxTitle)!, // non-null: guarded above
                Author: FirstOf(info, "authors"),
                Isbn: ExtractIsbn(info),
                CoverImageUrl: ExtractCover(info),
                Description: Truncate(GetString(info, "description"), MaxDescription),
                Genre: FirstOf(info, "categories")));
        }

        // Surface books that have an ISBN first — only those can be bulk-created. OrderBy is a
        // stable sort, so the original Google relevance order is preserved within each group.
        return results
            .OrderByDescending(r => !string.IsNullOrWhiteSpace(r.Isbn))
            .ToList();
    }

    private static string? ExtractIsbn(JsonElement info)
    {
        if (!info.TryGetProperty("industryIdentifiers", out var ids) ||
            ids.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        // Prefer ISBN_13, fall back to ISBN_10.
        string? isbn13 = null;
        string? isbn10 = null;
        foreach (var id in ids.EnumerateArray())
        {
            var type = GetString(id, "type");
            var value = GetString(id, "identifier");
            if (type == "ISBN_13") isbn13 = value;
            else if (type == "ISBN_10") isbn10 = value;
        }

        return Truncate(isbn13 ?? isbn10, MaxIsbn);
    }

    private static string? ExtractCover(JsonElement info)
    {
        if (!info.TryGetProperty("imageLinks", out var links) ||
            links.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var url = GetString(links, "thumbnail") ?? GetString(links, "smallThumbnail");
        // Google returns http:// thumbnails; force https so the [Url] cover passes and loads.
        if (url is not null && url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            url = string.Concat("https://", url.AsSpan("http://".Length));
        }

        return url;
    }

    private static string? FirstOf(JsonElement info, string property)
    {
        if (info.TryGetProperty(property, out var array) &&
            array.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in array.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    return element.GetString();
                }
            }
        }

        return null;
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? Truncate(string? value, int maxLength) =>
        value is { Length: > 0 } && value.Length > maxLength
            ? value[..maxLength]
            : value;
}
