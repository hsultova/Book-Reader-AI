namespace BookReaderApp.Models.ViewModels;

// Drives the reviews section on a book's detail page: the community review list plus the
// signed-in user's write/edit form (shown only when CanReview).
public class BookReviewsViewModel
{
    public int BookId { get; set; }

    // Every review for the book, newest first.
    public IReadOnlyList<Review> Reviews { get; set; } = Array.Empty<Review>();

    // The current user's own review, or null if they haven't written one.
    public Review? UserReview { get; set; }

    // True when the current user may write a review (authenticated and the book is on their shelf).
    public bool CanReview { get; set; }

    // The current user's id, used to mark their own review within the list. Null when anonymous.
    public string? CurrentUserId { get; set; }

    // Local URL to return to after a review change (preserves the current page).
    public string? ReturnUrl { get; set; }
}
