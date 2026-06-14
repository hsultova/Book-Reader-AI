namespace BookReaderApp.Models.ViewModels;

// Drives the "My Books" page: the (optionally filtered) shelf entries plus
// per-shelf counts for the left sidebar.
public class MyBooksViewModel
{
    public IReadOnlyList<UserBook> Books { get; set; } = Array.Empty<UserBook>();

    public ReadingStatus? SelectedStatus { get; set; }

    public int TotalCount { get; set; }

    public IReadOnlyDictionary<ReadingStatus, int> Counts { get; set; } =
        new Dictionary<ReadingStatus, int>();
}
