namespace BookReaderApp.Models;

// A reader's comment on someone else's review. Users cannot comment on their own review.
// Authors may delete their own comments.
public class ReviewComment
{
    public int Id { get; set; }

    public int ReviewId { get; set; }

    public Review? Review { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
