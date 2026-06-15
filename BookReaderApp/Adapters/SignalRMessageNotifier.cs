using BookReaderApp.Hubs;
using BookReaderApp.Messaging.Abstractions;
using BookReaderApp.Messaging.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace BookReaderApp.Adapters;

// Implements the messaging module's real-time seam over SignalR. Targets a specific user
// by id via Clients.User, which works because Identity sets the NameIdentifier claim that
// SignalR's default IUserIdProvider keys on.
public class SignalRMessageNotifier : IMessageNotifier
{
    private readonly IHubContext<ChatHub> _hub;

    public SignalRMessageNotifier(IHubContext<ChatHub> hub)
    {
        _hub = hub;
    }

    public Task MessageSentAsync(string recipientId, MessageDto message) =>
        _hub.Clients.User(recipientId).SendAsync(ChatHub.MessageReceived, message);

    public Task UnreadCountChangedAsync(string userId, int unreadCount) =>
        _hub.Clients.User(userId).SendAsync(ChatHub.UnreadCountChanged, unreadCount);
}
