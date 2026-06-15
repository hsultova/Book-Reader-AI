namespace BookReaderApp.Models.ViewModels;

// One row in the conversation list: the messaging DTO enriched with the counterpart's
// display name and avatar (which the messaging module, knowing only user ids, can't supply).
public record ConversationListItem(
    int ConversationId,
    string OtherUserId,
    string OtherDisplayName,
    string? OtherProfilePicturePath,
    string? LastMessagePreview,
    DateTime LastMessageAt,
    bool LastMessageFromMe,
    int UnreadCount);

// The inbox: every conversation the current user takes part in.
public class MessagesViewModel
{
    public IReadOnlyList<ConversationListItem> Conversations { get; init; } = [];

    public int TotalUnread => Conversations.Sum(c => c.UnreadCount);
}
