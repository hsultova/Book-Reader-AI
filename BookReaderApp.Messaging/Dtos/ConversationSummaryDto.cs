namespace BookReaderApp.Messaging.Dtos;

// One row in a user's conversation list. OtherUserId identifies the counterpart; the host
// resolves it to a display name and avatar.
public record ConversationSummaryDto(
    int ConversationId,
    string OtherUserId,
    string? LastMessagePreview,
    DateTime LastMessageAt,
    bool LastMessageFromMe,
    int UnreadCount);
