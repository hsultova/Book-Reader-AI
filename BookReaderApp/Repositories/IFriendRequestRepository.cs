using BookReaderApp.Models;

namespace BookReaderApp.Repositories;

// Specialized repository for friend requests. Extends generic CRUD with queries for
// resolving the relationship between two users and listing a user's friends/requests.
public interface IFriendRequestRepository : IRepository<FriendRequest>
{
    // The single request between two users in either direction (Requester/Addressee
    // swapped), or null. The unique-per-ordered-pair index allows at most one each way.
    Task<FriendRequest?> GetBetweenAsync(string userAId, string userBId);

    // Accepted friendships involving the user, with both users loaded.
    Task<IReadOnlyList<FriendRequest>> GetAcceptedForUserAsync(string userId);

    // Pending requests the user has received, requester loaded, newest first.
    Task<IReadOnlyList<FriendRequest>> GetIncomingPendingAsync(string userId);

    // Pending requests the user has sent, addressee loaded, newest first.
    Task<IReadOnlyList<FriendRequest>> GetOutgoingPendingAsync(string userId);
}
