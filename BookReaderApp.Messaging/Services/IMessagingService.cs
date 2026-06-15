using BookReaderApp.Messaging.Dtos;

namespace BookReaderApp.Messaging.Services;

// Entry point for the messaging module. All friendship and participant checks live here,
// so controllers stay thin. Authorization violations are silent no-ops (matching the
// FriendRequestService convention) rather than thrown exceptions.
public interface IMessagingService
{
    // The user's conversations, most-recently-active first.
    Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsAsync(string userId);

    // A single conversation the user takes part in, or null if it doesn't exist or the
    // user isn't a participant.
    Task<ConversationSummaryDto?> GetConversationAsync(string userId, int conversationId);

    // Opens (or creates) the conversation between two friends. Returns null if they
    // aren't friends or the ids are the same.
    Task<int?> GetOrCreateConversationAsync(string userId, string otherUserId);

    // Messages in a conversation the user takes part in, oldest-first. Empty if the
    // user isn't a participant or the conversation doesn't exist.
    Task<IReadOnlyList<MessageDto>> GetThreadAsync(string userId, int conversationId, int take = DefaultThreadPageSize);

    // Sends a message to a friend, creating the conversation on first contact. Returns the
    // persisted message, or null if the two users aren't friends or the content is empty.
    Task<MessageDto?> SendMessageAsync(string senderId, string recipientId, string content);

    // Marks the other party's messages in the conversation as read (no-op if the user
    // isn't a participant). Returns the user's new total unread count.
    Task<int> MarkReadAsync(string userId, int conversationId);

    Task<int> GetUnreadCountAsync(string userId);

    const int DefaultThreadPageSize = 50;
}
