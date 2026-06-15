namespace BookReaderApp.Messaging.Models;

// A single message within a Conversation. SenderId is stored as a plain user-id string:
// the library deliberately has no FK/navigation to ApplicationUser (which lives in the
// web project) so it stays standalone.
public class DirectMessage
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    public Conversation? Conversation { get; set; }

    // The user who sent this message (one of the conversation's two participants).
    public string SenderId { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Set when the recipient (the participant who isn't the sender) opens the thread.
    public DateTime? ReadAt { get; set; }
}
