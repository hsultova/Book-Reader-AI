namespace BookReaderApp.Services;

// Transport-agnostic outcomes for book mutations. Mirrors AccountResults: the service
// reports what happened; the controller decides how to render it (view, redirect, 404).

public sealed record BookSaveResult(bool Succeeded, int? BookId, IReadOnlyList<string> Errors)
{
    public static BookSaveResult Success(int bookId) =>
        new(true, bookId, Array.Empty<string>());

    public static BookSaveResult NotFound() =>
        new(false, null, new[] { "The requested book was not found." });

    public static BookSaveResult Failure(IEnumerable<string> errors) =>
        new(false, null, errors.ToList());
}
