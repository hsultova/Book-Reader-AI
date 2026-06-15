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

// The Friends page: established friends plus pending requests in both directions.
public class FriendsViewModel
{
    public IReadOnlyList<FriendListItem> Friends { get; init; } = [];

    public IReadOnlyList<FriendListItem> IncomingRequests { get; init; } = [];

    public IReadOnlyList<FriendListItem> OutgoingRequests { get; init; } = [];
}
