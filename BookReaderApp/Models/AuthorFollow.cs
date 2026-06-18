namespace BookReaderApp.Models;

// Tracks a user following an author. One-way: following an author means the user
// is interested in their work; no messaging or approval is implied.
public class AuthorFollow
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public int AuthorId { get; set; }

    public Author? Author { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
