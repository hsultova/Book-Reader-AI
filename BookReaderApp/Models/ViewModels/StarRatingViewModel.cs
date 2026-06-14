namespace BookReaderApp.Models.ViewModels;

// Drives the reusable star-rating widget shown per book on the catalog, details, and
// My Books pages. Always shows the community average; shows interactive stars when CanRate.
public class StarRatingViewModel
{
    public int BookId { get; set; }

    // The signed-in user's own 1–5 rating for this book, or null if unrated.
    public int? UserRating { get; set; }

    // The community average across all users, or null if no one has rated the book yet.
    public RatingSummary? Summary { get; set; }

    // Local URL to return to after a rating change (preserves the current page/filter).
    public string? ReturnUrl { get; set; }

    // True when the current user may set a rating (authenticated and the book is on their shelf).
    public bool CanRate { get; set; }
}
