namespace BookReaderApp.Models.ViewModels;

// A single message rendered in a thread. IsMine drives left/right bubble alignment.
public record ThreadMessageItem(
    int Id,
    string SenderId,
    string Content,
    DateTime CreatedAt,
    bool IsMine);

// The open conversation: who it's with, the current user's id (so the live SignalR client
// can tell incoming messages apart), and the messages oldest-first.
public class ThreadViewModel
{
    public int ConversationId { get; init; }

    public string CurrentUserId { get; init; } = string.Empty;

    public string OtherUserId { get; init; } = string.Empty;

    public string OtherDisplayName { get; init; } = string.Empty;

    public string? OtherProfilePicturePath { get; init; }

    public IReadOnlyList<ThreadMessageItem> Messages { get; init; } = [];
}
