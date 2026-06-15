namespace BookReaderApp.Models;

// A reader's "like" on someone else's review. At most one like per user per review
// (enforced by a unique index on ReviewId + UserId). Users cannot like their own review.
public class ReviewLike
{
    public int Id { get; set; }

    public int ReviewId { get; set; }

    public Review? Review { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
