namespace BookReaderApp.Models;

// A user's text review of a book. At most one review per user per book (enforced by a
// unique index on UserId + BookId). Reviews are shown to everyone on the book's detail
// page; spoiler reviews are hidden behind a click-to-reveal control in the UI.
public class Review
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public int BookId { get; set; }

    public Book? Book { get; set; }

    public string Text { get; set; } = string.Empty;

    // True when the review body may contain spoilers and should stay hidden until revealed.
    public bool ContainsSpoilers { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
