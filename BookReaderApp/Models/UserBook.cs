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

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
