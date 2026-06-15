using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BookReaderApp.Hubs;

// Marker hub for server -> client push only (clients never invoke methods on it).
// SignalR's default user-id provider keys connections by the NameIdentifier claim, which
// is the Identity user id, so Clients.User(userId) reaches exactly that person's tabs.
[Authorize]
public class ChatHub : Hub
{
    // Event names the server raises on this hub; kept here so the notifier and any future
    // callers share one source of truth.
    public const string MessageReceived = "MessageReceived";
    public const string UnreadCountChanged = "UnreadCountChanged";
}
