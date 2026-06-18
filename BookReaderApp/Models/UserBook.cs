namespace BookReaderApp.Models;

// A book on a specific user's shelf — the join between ApplicationUser and Book.
// A user sees their UserBooks on the "My Books" view.
public class UserBook
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public int BookId { get; set; }

    public Book? Book { get; set; }

    // A book sits on exactly one shelf: either a built-in Status or a custom Shelf.
    // Exactly one of Status / ShelfId is set.
    public ReadingStatus? Status { get; set; }

    public int? ShelfId { get; set; }

    public Shelf? Shelf { get; set; }

    // The user's 1–5 star rating of the book, or null if unrated. Independent of shelf
    // placement: a book can be rated on any shelf (or none of the built-in statuses).
    public int? Rating { get; set; }

    // When the current Rating was last set, or null when unrated. Distinct from AddedAt
    // so the activity feed can place a rating at the moment it happened, not the shelf add.
    public DateTime? RatedAt { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // When the book was marked Finished, or null if it has never been finished. Cleared
    // when the book leaves the Finished status. Drives the annual reading challenge.
    public DateTime? FinishedAt { get; set; }
}
