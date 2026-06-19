namespace BookReaderApp.Services;

// A single book match returned from the Google Books "volumes" search, projected to the
// fields the create form needs. All optional fields may be null when the volume omits them.
public sealed record GoogleBookResult(
    string Title,
    string? Author,
    string? Isbn,
    string? CoverImageUrl,
    string? Description,
    IReadOnlyList<string> Genres);

public interface IGoogleBooksService
{
    // Searches public Google Books volumes. Returns an empty list (never throws to the caller)
    // when the query is blank, no API key is configured, or the upstream call fails.
    Task<IReadOnlyList<GoogleBookResult>> SearchAsync(string query, CancellationToken ct = default);
}
