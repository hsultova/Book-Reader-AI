using BookReaderApp.Models.ViewModels;

namespace BookReaderApp.Services;

// Business logic for friend requests: sending, responding, and resolving the
// relationship state that drives the profile button and the Friends page.
public interface IFriendRequestService
{
    // Relationship of otherUserId relative to currentUserId (for the profile button).
    Task<FriendState> GetRelationshipAsync(string currentUserId, string otherUserId);

    // The pending request id between the two users, if currentUserId is the addressee
    // (so the profile can render Accept/Reject). Null otherwise.
    Task<int?> GetIncomingRequestIdAsync(string currentUserId, string otherUserId);

    // Sends a request from requesterId to addresseeId. No-op if it's a self-request or a
    // Pending/Accepted relationship already exists. A prior Rejected row is reused.
    Task SendRequestAsync(string requesterId, string addresseeId);

    // Accepts a pending request. Only the addressee may accept.
    Task AcceptAsync(int requestId, string currentUserId);

    // Rejects a pending request. Only the addressee may reject.
    Task RejectAsync(int requestId, string currentUserId);

    // Cancels (deletes) a pending request the current user sent. Only the requester may cancel.
    Task CancelAsync(int requestId, string currentUserId);

    // Removes an accepted friendship between the two users. Either participant may remove it.
    // No-op if they are not friends. Any follow between them is left intact.
    Task RemoveFriendAsync(string currentUserId, string otherUserId);

    // The user ids of everyone the current user is accepted friends with.
    Task<IReadOnlyList<string>> GetFriendIdsAsync(string currentUserId);

    // Builds the Friends page (friends + incoming + outgoing pending requests).
    // When searchQuery is set, also returns matching registered users (with their
    // relationship state) so new friend requests can be sent from the results.
    Task<FriendsViewModel> GetFriendsPageAsync(string currentUserId, string? searchQuery = null);
}
