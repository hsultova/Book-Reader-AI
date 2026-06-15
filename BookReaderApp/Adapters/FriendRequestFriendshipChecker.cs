using BookReaderApp.Messaging.Abstractions;
using BookReaderApp.Services;

namespace BookReaderApp.Adapters;

// Bridges the messaging module's friendship seam to the app's existing friend-request
// service, so messaging needs no reference back to the web project's friendship code.
public class FriendRequestFriendshipChecker : IFriendshipChecker
{
    private readonly IFriendRequestService _friendRequests;

    public FriendRequestFriendshipChecker(IFriendRequestService friendRequests)
    {
        _friendRequests = friendRequests;
    }

    public async Task<bool> AreFriendsAsync(string userIdA, string userIdB)
    {
        var friendIds = await _friendRequests.GetFriendIdsAsync(userIdA);
        return friendIds.Contains(userIdB);
    }
}
