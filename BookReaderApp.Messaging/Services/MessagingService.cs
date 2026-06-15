using BookReaderApp.Messaging.Abstractions;
using BookReaderApp.Messaging.Dtos;
using BookReaderApp.Messaging.Models;
using BookReaderApp.Messaging.Repositories;
using Microsoft.Extensions.Logging;

namespace BookReaderApp.Messaging.Services;

public class MessagingService : IMessagingService
{
    // Hard cap on a single message so the column and the UI stay bounded.
    private const int MaxMessageLength = 2000;

    private readonly IConversationRepository _conversations;
    private readonly IDirectMessageRepository _messages;
    private readonly IFriendshipChecker _friendship;
    private readonly IMessageNotifier _notifier;
    private readonly ILogger<MessagingService> _logger;

    public MessagingService(
        IConversationRepository conversations,
        IDirectMessageRepository messages,
        IFriendshipChecker friendship,
        IMessageNotifier notifier,
        ILogger<MessagingService> logger)
    {
        _conversations = conversations;
        _messages = messages;
        _friendship = friendship;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsAsync(string userId)
    {
        var conversations = await _conversations.GetForUserAsync(userId);

        var summaries = new List<ConversationSummaryDto>(conversations.Count);
        foreach (var conversation in conversations)
        {
            var last = await _messages.GetLastForConversationAsync(conversation.Id);
            var unread = await _messages.CountUnreadInConversationAsync(conversation.Id, userId);

            summaries.Add(new ConversationSummaryDto(
                conversation.Id,
                conversation.OtherParticipant(userId),
                last?.Content,
                conversation.LastMessageAt,
                LastMessageFromMe: last is not null && last.SenderId == userId,
                unread));
        }

        return summaries;
    }

    public async Task<ConversationSummaryDto?> GetConversationAsync(string userId, int conversationId)
    {
        var conversation = await _conversations.GetByIdAsync(conversationId);
        if (conversation is null || !conversation.HasParticipant(userId))
        {
            return null;
        }

        var last = await _messages.GetLastForConversationAsync(conversation.Id);
        var unread = await _messages.CountUnreadInConversationAsync(conversation.Id, userId);

        return new ConversationSummaryDto(
            conversation.Id,
            conversation.OtherParticipant(userId),
            last?.Content,
            conversation.LastMessageAt,
            LastMessageFromMe: last is not null && last.SenderId == userId,
            unread);
    }

    public async Task<int?> GetOrCreateConversationAsync(string userId, string otherUserId)
    {
        if (userId == otherUserId || !await _friendship.AreFriendsAsync(userId, otherUserId))
        {
            return null;
        }

        var conversation = await GetOrCreateAsync(userId, otherUserId);
        return conversation.Id;
    }

    public async Task<IReadOnlyList<MessageDto>> GetThreadAsync(
        string userId, int conversationId, int take = IMessagingService.DefaultThreadPageSize)
    {
        var conversation = await _conversations.GetByIdAsync(conversationId);
        if (conversation is null || !conversation.HasParticipant(userId))
        {
            return [];
        }

        var messages = await _messages.GetThreadAsync(conversationId, take);
        return messages.Select(ToDto).ToList();
    }

    public async Task<MessageDto?> SendMessageAsync(string senderId, string recipientId, string content)
    {
        var trimmed = content?.Trim();
        if (string.IsNullOrEmpty(trimmed) || senderId == recipientId)
        {
            return null;
        }

        if (!await _friendship.AreFriendsAsync(senderId, recipientId))
        {
            _logger.LogWarning("Blocked message from {Sender} to non-friend {Recipient}.", senderId, recipientId);
            return null;
        }

        if (trimmed.Length > MaxMessageLength)
        {
            trimmed = trimmed[..MaxMessageLength];
        }

        var conversation = await GetOrCreateAsync(senderId, recipientId);

        var message = new DirectMessage
        {
            ConversationId = conversation.Id,
            SenderId = senderId,
            Content = trimmed,
            CreatedAt = DateTime.UtcNow
        };
        await _messages.AddAsync(message);

        conversation.LastMessageAt = message.CreatedAt;
        _conversations.Update(conversation);

        await _messages.SaveChangesAsync();
        _logger.LogInformation("Message {MessageId} sent in conversation {ConversationId}.", message.Id, conversation.Id);

        var dto = ToDto(message);

        // Push the new message to the recipient and refresh both parties' unread badges.
        await _notifier.MessageSentAsync(recipientId, dto);
        await _notifier.UnreadCountChangedAsync(recipientId, await _messages.CountUnreadForUserAsync(recipientId));

        return dto;
    }

    public async Task<int> MarkReadAsync(string userId, int conversationId)
    {
        var conversation = await _conversations.GetByIdAsync(conversationId);
        if (conversation is null || !conversation.HasParticipant(userId))
        {
            return await _messages.CountUnreadForUserAsync(userId);
        }

        var changed = await _messages.MarkReadAsync(conversationId, userId);
        if (changed > 0)
        {
            await _messages.SaveChangesAsync();
        }

        var unread = await _messages.CountUnreadForUserAsync(userId);
        if (changed > 0)
        {
            await _notifier.UnreadCountChangedAsync(userId, unread);
        }

        return unread;
    }

    public Task<int> GetUnreadCountAsync(string userId) =>
        _messages.CountUnreadForUserAsync(userId);

    // Finds the existing conversation for the pair or creates a normalized one. Callers
    // must have already verified the two users are friends.
    private async Task<Conversation> GetOrCreateAsync(string userAId, string userBId)
    {
        var existing = await _conversations.GetBetweenAsync(userAId, userBId);
        if (existing is not null)
        {
            return existing;
        }

        var (user1Id, user2Id) = Conversation.NormalizePair(userAId, userBId);
        var conversation = new Conversation
        {
            User1Id = user1Id,
            User2Id = user2Id,
            CreatedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow
        };
        await _conversations.AddAsync(conversation);
        await _conversations.SaveChangesAsync();
        return conversation;
    }

    private static MessageDto ToDto(DirectMessage m) =>
        new(m.Id, m.ConversationId, m.SenderId, m.Content, m.CreatedAt, m.ReadAt is not null);
}
