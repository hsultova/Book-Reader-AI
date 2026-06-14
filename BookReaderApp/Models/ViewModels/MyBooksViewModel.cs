namespace BookReaderApp.Models.ViewModels;

// Drives the "My Books" page: the (optionally filtered) shelf entries plus
// per-shelf counts for the left sidebar (built-in statuses and custom shelves).
public class MyBooksViewModel
{
    public IReadOnlyList<UserBook> Books { get; set; } = Array.Empty<UserBook>();

    public ReadingStatus? SelectedStatus { get; set; }

    public int? SelectedShelfId { get; set; }

    // The active free-text search term across title, author, ISBN and keywords, if any.
    public string? SearchQuery { get; set; }

    public int TotalCount { get; set; }

    // Counts per built-in status.
    public IReadOnlyDictionary<ReadingStatus, int> Counts { get; set; } =
        new Dictionary<ReadingStatus, int>();

    // The user's custom shelves.
    public IReadOnlyList<Shelf> CustomShelves { get; set; } = Array.Empty<Shelf>();

    // Counts per custom shelf, keyed by shelf id.
    public IReadOnlyDictionary<int, int> ShelfCounts { get; set; } =
        new Dictionary<int, int>();

    // Community average rating per listed book, keyed by book id. Books with no ratings are absent.
    public IReadOnlyDictionary<int, RatingSummary> RatingSummaries { get; set; } =
        new Dictionary<int, RatingSummary>();
}
