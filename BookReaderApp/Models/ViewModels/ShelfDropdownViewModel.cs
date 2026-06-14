namespace BookReaderApp.Models.ViewModels;

// Drives the reusable shelf split-button dropdown shown per book on the catalog,
// details, and My Books pages.
public class ShelfDropdownViewModel
{
    public int BookId { get; set; }

    // The book's current shelf for this user, or null if it isn't shelved yet.
    public ReadingStatus? CurrentStatus { get; set; }

    // Local URL to return to after a status change/remove (preserves the current page/filter).
    public string? ReturnUrl { get; set; }
}
