namespace BookReaderApp.Messaging.Abstractions;

// Seam that lets the messaging module enforce "friends only" without referencing the web
// project (where friendship lives). The host implements this by delegating to its existing
// friend-request service, avoiding a circular dependency.
public interface IFriendshipChecker
{
    Task<bool> AreFriendsAsync(string userIdA, string userIdB);
}
