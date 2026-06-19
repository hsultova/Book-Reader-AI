using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BookReaderApp.Configuration;
using BookReaderApp.Models;
using Microsoft.Extensions.Options;

namespace BookReaderApp.Services;

// Gemini-backed implementation of the recommendation seam. Calls the Google Generative
// Language REST API (generateContent) with the reader's taste profile and a pool of candidate
// catalog books, asking — via structured JSON output (responseSchema) — for a ranked set of
// picks with a one-line reason each. The model only ever sees (and is told to choose from) the
// candidate ids we supply, so it cannot invent a book.
//
// The API key is attached server-side and never reaches the browser. Any failure (no API key,
// network error, malformed JSON) degrades to an empty result and is logged; the Books page
// then renders exactly as it would without AI, like GoogleBooksService.
public class GeminiBookRecommendationEngine : IBookRecommendationEngine
{
    // Keep descriptions short so a large candidate pool stays within a small token budget.
    private const int MaxDescriptionChars = 300;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiBookRecommendationEngine> _logger;

    public GeminiBookRecommendationEngine(
        HttpClient http,
        IOptions<GeminiOptions> options,
        ILogger<GeminiBookRecommendationEngine> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BookPick>> SuggestAsync(
        IReadOnlyList<Book> likedBooks,
        IReadOnlyList<Book> candidates,
        int max,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("AI recommendations skipped: no Gemini API key configured.");
            return Array.Empty<BookPick>();
        }

        if (candidates.Count == 0)
        {
            return Array.Empty<BookPick>();
        }

        try
        {
            var baseUrl = _options.BaseUrl.TrimEnd('/');
            var requestUri =
                $"{baseUrl}/models/{Uri.EscapeDataString(_options.Model)}:generateContent?key={Uri.EscapeDataString(_options.ApiKey)}";

            using var response = await _http.PostAsJsonAsync(
                requestUri, BuildRequest(likedBooks, candidates, max), ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var json = ExtractText(document);
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<BookPick>();
            }

            var parsed = JsonSerializer.Deserialize<RecommendationResponse>(json, JsonOptions);
            return parsed?.Recommendations?
                .Where(r => !string.IsNullOrWhiteSpace(r.Reason))
                .Select(r => new BookPick(r.BookId, r.Reason!.Trim()))
                .ToList()
                ?? (IReadOnlyList<BookPick>)Array.Empty<BookPick>();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            // Never let an upstream/network/parsing failure reach the page — degrade to no AI rows.
            _logger.LogError(ex, "AI recommendation request to Gemini failed.");
            return Array.Empty<BookPick>();
        }
    }

    // Pulls the first candidate's text part out of a generateContent response.
    private static string? ExtractText(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("candidates", out var candidates) ||
            candidates.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var candidate in candidates.EnumerateArray())
        {
            if (candidate.TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.ValueKind == JsonValueKind.Array)
            {
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var text) &&
                        text.ValueKind == JsonValueKind.String)
                    {
                        return text.GetString();
                    }
                }
            }
        }

        return null;
    }

    // Builds the generateContent request body: a system instruction, the user prompt, and a
    // generationConfig that forces JSON output matching our recommendations schema.
    private static object BuildRequest(
        IReadOnlyList<Book> likedBooks, IReadOnlyList<Book> candidates, int max)
    {
        const string system =
            "You are a knowledgeable, friendly librarian recommending books to a reader. " +
            "You are given the books the reader has rated highly and a numbered list of " +
            "candidate books. Choose the candidates this particular reader is most likely to " +
            "enjoy. Rules: only ever choose from the supplied candidate ids — never invent a " +
            "book or id; pick at most the requested number; for each pick write one warm, " +
            "specific sentence (max ~25 words) explaining why it fits THIS reader's taste, " +
            "referencing what they already liked when relevant.";

        return new
        {
            system_instruction = new { parts = new[] { new { text = system } } },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = BuildPrompt(likedBooks, candidates, max) } }
                }
            },
            generationConfig = new
            {
                maxOutputTokens = 2000,
                responseMimeType = "application/json",
                // Gemini's responseSchema is an OpenAPI subset (uppercase type names).
                responseSchema = new
                {
                    type = "OBJECT",
                    properties = new
                    {
                        recommendations = new
                        {
                            type = "ARRAY",
                            items = new
                            {
                                type = "OBJECT",
                                properties = new
                                {
                                    bookId = new { type = "INTEGER" },
                                    reason = new { type = "STRING" }
                                },
                                required = new[] { "bookId", "reason" }
                            }
                        }
                    },
                    required = new[] { "recommendations" }
                }
            }
        };
    }

    private static string BuildPrompt(
        IReadOnlyList<Book> likedBooks, IReadOnlyList<Book> candidates, int max)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Books this reader rated highly:");
        if (likedBooks.Count == 0)
        {
            sb.AppendLine("- (none yet — recommend broadly appealing, well-regarded books)");
        }
        else
        {
            foreach (var book in likedBooks)
            {
                sb.Append("- \"").Append(book.Title).Append('"');
                if (book.Author is not null)
                {
                    sb.Append(" by ").Append(book.Author.Name);
                }
                if (!string.IsNullOrWhiteSpace(book.Genre?.Name))
                {
                    sb.Append(" [").Append(book.Genre!.Name).Append(']');
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine();
        sb.Append("Candidate books (choose up to ").Append(max).AppendLine(" by id):");
        foreach (var book in candidates)
        {
            sb.Append("id ").Append(book.Id).Append(": \"").Append(book.Title).Append('"');
            if (book.Author is not null)
            {
                sb.Append(" by ").Append(book.Author.Name);
            }
            if (!string.IsNullOrWhiteSpace(book.Genre?.Name))
            {
                sb.Append(" [").Append(book.Genre!.Name).Append(']');
            }
            if (!string.IsNullOrWhiteSpace(book.Description))
            {
                sb.Append(" — ").Append(Shorten(book.Description!));
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string Shorten(string text) =>
        text.Length > MaxDescriptionChars ? text[..MaxDescriptionChars] + "…" : text;

    // Shape of the structured JSON the model returns (see the schema above).
    private sealed class RecommendationResponse
    {
        public List<RecommendationItem>? Recommendations { get; set; }
    }

    private sealed class RecommendationItem
    {
        public int BookId { get; set; }
        public string? Reason { get; set; }
    }
}
