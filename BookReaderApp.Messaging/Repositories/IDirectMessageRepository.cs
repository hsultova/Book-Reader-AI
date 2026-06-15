using BookReaderApp.Messaging.Models;

namespace BookReaderApp.Messaging.Repositories;

public interface IDirectMessageRepository
{
    Task AddAsync(DirectMessage message);

    // The most recent `take` messages in a conversation, returned oldest-first so the
    // view can render top-to-bottom without re-sorting.
    Task<IReadOnlyList<DirectMessage>> GetThreadAsync(int conversationId, int take);

    // The most recent message in a conversation, or null if it has none yet.
    Task<DirectMessage?> GetLastForConversationAsync(int conversationId);

    // Unread messages in a single conversation addressed to the user.
    Task<int> CountUnreadInConversationAsync(int conversationId, string userId);

    // Total unread messages addressed to the user across all their conversations
    // (messages they didn't send and haven't read yet).
    Task<int> CountUnreadForUserAsync(string userId);

    // Marks every unread message in the conversation that the reader didn't send as read,
    // returning how many rows changed. Caller is responsible for SaveChangesAsync.
    Task<int> MarkReadAsync(int conversationId, string readerUserId);

    Task<int> SaveChangesAsync();
}
