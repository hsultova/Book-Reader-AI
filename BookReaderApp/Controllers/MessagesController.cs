using BookReaderApp.Messaging.Dtos;
using BookReaderApp.Messaging.Services;
using BookReaderApp.Models;
using BookReaderApp.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookReaderApp.Controllers;

[Authorize]
public class MessagesController : Controller
{
    private readonly IMessagingService _messaging;
    private readonly UserManager<ApplicationUser> _userManager;

    public MessagesController(
        IMessagingService messaging,
        UserManager<ApplicationUser> userManager)
    {
        _messaging = messaging;
        _userManager = userManager;
    }

    // The inbox: every conversation, newest activity first.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var summaries = await _messaging.GetConversationsAsync(userId);

        var items = new List<ConversationListItem>(summaries.Count);
        foreach (var s in summaries)
        {
            var other = await _userManager.FindByIdAsync(s.OtherUserId);
            items.Add(new ConversationListItem(
                s.ConversationId,
                s.OtherUserId,
                other?.DisplayName ?? "Unknown",
                other?.ProfilePicturePath,
                s.LastMessagePreview,
                s.LastMessageAt,
                s.LastMessageFromMe,
                s.UnreadCount));
        }

        return View(new MessagesViewModel { Conversations = items });
    }

    // Opens (or starts) the conversation with a friend, then redirects to the thread.
    // Used by the "Message" button on a friend's profile.
    [HttpPost]
    public async Task<IActionResult> Open(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest();
        }

        var userId = _userManager.GetUserId(User)!;
        var conversationId = await _messaging.GetOrCreateConversationAsync(userId, id);
        if (conversationId is null)
        {
            // Not friends (or self): nothing to open.
            return RedirectToAction("Index", "Profile", new { id });
        }

        return RedirectToAction(nameof(Thread), new { id = conversationId.Value });
    }

    // Displays a conversation and marks the other party's messages read.
    [HttpGet]
    public async Task<IActionResult> Thread(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var conversation = await _messaging.GetConversationAsync(userId, id);
        if (conversation is null)
        {
            return RedirectToAction(nameof(Index));
        }

        await _messaging.MarkReadAsync(userId, id);

        var other = await _userManager.FindByIdAsync(conversation.OtherUserId);
        var messages = await _messaging.GetThreadAsync(userId, id);

        return View(new ThreadViewModel
        {
            ConversationId = id,
            CurrentUserId = userId,
            OtherUserId = conversation.OtherUserId,
            OtherDisplayName = other?.DisplayName ?? "Unknown",
            OtherProfilePicturePath = other?.ProfilePicturePath,
            Messages = messages
                .Select(m => new ThreadMessageItem(m.Id, m.SenderId, m.Content, m.CreatedAt, m.SenderId == userId))
                .ToList()
        });
    }

    // Posts a message. Returns the persisted message as JSON so the page can append it
    // without a reload; the recipient gets it live over SignalR.
    [HttpPost]
    public async Task<IActionResult> Send(string recipientId, string content)
    {
        var userId = _userManager.GetUserId(User)!;
        var message = await _messaging.SendMessageAsync(userId, recipientId, content);
        if (message is null)
        {
            return BadRequest();
        }

        return Json(message);
    }

    // Marks a conversation read (used when a message arrives while the thread is open, so
    // the unread badge doesn't linger). Returns the user's new total unread count.
    [HttpPost]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var unread = await _messaging.MarkReadAsync(userId, id);
        return Json(new { unread });
    }
}
