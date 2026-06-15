namespace BookReaderApp.Models.ViewModels;

// Relationship between the current viewer and another user, used to render the
// state-aware friend button on a profile.
public enum FriendState
{
    None,             // no request either way -> "Add friend"
    OutgoingPending,  // current user sent a request -> "Request sent"
    IncomingPending,  // current user received a request -> "Accept" / "Reject"
    Friends           // accepted -> "Friends"
}

// One person in a Friends-page list. RequestId is set only for pending rows so the
// Accept/Reject forms can target the right request.
public record FriendListItem(
    string UserId,
    string DisplayName,
    string? ProfilePicturePath,
    int? RequestId);

// A registered user matching a directory search, with the relationship to the viewer
// so the right action (Add friend / Request sent / Accept-Reject / Friends) can render.
// RequestId is set only when the viewer has an incoming pending request from this user.
public record FriendSearchResultItem(
    string UserId,
    string DisplayName,
    string? ProfilePicturePath,
    FriendState State,
    int? RequestId);

// The Friends page: established friends plus pending requests in both directions, and
// (when a search is active) matching registered users to send new requests to.
public class FriendsViewModel
{
    // The active free-text search term across registered users, if any.
    public string? SearchQuery { get; init; }

    // Registered users matching SearchQuery (empty when no search is active).
    public IReadOnlyList<FriendSearchResultItem> SearchResults { get; init; } = [];

    public IReadOnlyList<FriendListItem> Friends { get; init; } = [];

    public IReadOnlyList<FriendListItem> IncomingRequests { get; init; } = [];

    public IReadOnlyList<FriendListItem> OutgoingRequests { get; init; } = [];
}
