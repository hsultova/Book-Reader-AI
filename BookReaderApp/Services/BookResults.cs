namespace BookReaderApp.Services;

// Transport-agnostic outcomes for book mutations. Mirrors AccountResults: the service
// reports what happened; the controller decides how to render it (view, redirect, 404).

public sealed record BookSaveResult(bool Succeeded, int? BookId, IReadOnlyList<string> Errors)
{
    public static BookSaveResult Success(int bookId) =>
        new(true, bookId, Array.Empty<string>());

    public static BookSaveResult NotFound() =>
        new(false, null, new[] { "The requested book was not found." });

    public static BookSaveResult Duplicate(string isbn) =>
        new(false, null, new[] { $"A book with ISBN '{isbn}' already exists in the catalog." });

    public static BookSaveResult Failure(IEnumerable<string> errors) =>
        new(false, null, errors.ToList());
}

// Outcome of a bulk create: how many books were persisted and which were skipped
// (e.g. for a missing ISBN) so the UI can report them back to the user.
public sealed record BulkBookSaveResult(int CreatedCount, IReadOnlyList<string> SkippedTitles);
