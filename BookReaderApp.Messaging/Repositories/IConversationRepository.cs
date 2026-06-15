using BookReaderApp.Messaging.Models;

namespace BookReaderApp.Messaging.Repositories;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(int id);

    // The single conversation between two users (participant order is normalized
    // internally), or null. Mirrors FriendRequestRepository.GetBetweenAsync.
    Task<Conversation?> GetBetweenAsync(string userAId, string userBId);

    // Conversations the user takes part in, most-recently-active first.
    Task<IReadOnlyList<Conversation>> GetForUserAsync(string userId);

    Task AddAsync(Conversation conversation);

    void Update(Conversation conversation);

    Task<int> SaveChangesAsync();
}
