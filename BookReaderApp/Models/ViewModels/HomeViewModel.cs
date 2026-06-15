namespace BookReaderApp.Models.ViewModels;

// The kind of friend activity an UpdateItem represents, which decides how the row is worded.
public enum UpdateKind
{
    Review,   // friend wrote a review
    ShelfAdd, // friend added a book to a shelf or built-in status
    Rating    // friend rated a book 1–5 stars
}

// One entry in the home-page Updates feed. Several fields are kind-specific and left null
// when they don't apply (ShelfLabel for ShelfAdd, Rating for Rating, ReviewSnippet for Review).
public record UpdateItem(
    UpdateKind Kind,
    string FriendDisplayName,
    string? FriendProfilePicturePath,
    int BookId,
    string BookTitle,
    DateTime Timestamp,
    string? ShelfLabel,
    int? Rating,
    string? ReviewSnippet,
    bool ContainsSpoilers);

// The home page: a chronological feed of recent activity from the viewer's friends.
public class HomeViewModel
{
    public IReadOnlyList<UpdateItem> Updates { get; init; } = [];
}
