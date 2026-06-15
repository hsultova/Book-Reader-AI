using BookReaderApp.Messaging.Dtos;

namespace BookReaderApp.Messaging.Abstractions;

// Seam for pushing real-time updates to connected clients. Implemented in the host via
// SignalR; the library stays free of any ASP.NET/SignalR dependency.
public interface IMessageNotifier
{
    // A new message was delivered to recipientId.
    Task MessageSentAsync(string recipientId, MessageDto message);

    // userId's total unread-message count changed (e.g. after a send or a read).
    Task UnreadCountChangedAsync(string userId, int unreadCount);
}
