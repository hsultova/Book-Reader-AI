namespace BookReaderApp.Models.ViewModels;

// Drives the reusable shelf split-button dropdown shown per book on the catalog,
// details, and My Books pages.
public class ShelfDropdownViewModel
{
    public int BookId { get; set; }

    // The book's current built-in status for this user, or null if not on a built-in shelf.
    public ReadingStatus? CurrentStatus { get; set; }

    // The book's current custom shelf for this user, or null if not on a custom shelf.
    public int? CurrentShelfId { get; set; }

    public string? CurrentShelfName { get; set; }

    // The user's custom shelves, offered as move targets in the dropdown.
    public IReadOnlyList<Shelf> CustomShelves { get; set; } = Array.Empty<Shelf>();

    // Local URL to return to after a status change/remove (preserves the current page/filter).
    public string? ReturnUrl { get; set; }
}
