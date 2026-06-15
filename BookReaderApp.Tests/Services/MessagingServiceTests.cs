using BookReaderApp.Messaging.Abstractions;
using BookReaderApp.Messaging.Data;
using BookReaderApp.Messaging.Dtos;
using BookReaderApp.Messaging.Repositories;
using BookReaderApp.Messaging.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookReaderApp.Tests.Services;

public class MessagingServiceTests
{
    private static MessagingDbContext NewContext() =>
        new(new DbContextOptionsBuilder<MessagingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // Builds a service over the given context. By default every pair is considered friends;
    // pass areFriends: false to simulate strangers. The notifier records its calls.
    private static (MessagingService Service, RecordingNotifier Notifier) NewService(
        MessagingDbContext context, bool areFriends = true)
    {
        var notifier = new RecordingNotifier();
        var service = new MessagingService(
            new ConversationRepository(context),
            new DirectMessageRepository(context),
            new StubFriendshipChecker(areFriends),
            notifier,
            NullLogger<MessagingService>.Instance);
        return (service, notifier);
    }

    [Fact]
    public async Task SendMessage_BetweenFriends_PersistsAndNotifies()
    {
        using var context = NewContext();
        var (service, notifier) = NewService(context);

        var message = await service.SendMessageAsync("alice", "bob", "  Hello Bob  ");

        Assert.NotNull(message);
        Assert.Equal("Hello Bob", message!.Content); // trimmed
        var stored = Assert.Single(context.DirectMessages);
        Assert.Equal("alice", stored.SenderId);
        Assert.Equal("bob", Assert.Single(notifier.MessagesSentTo));
        Assert.Contains("bob", notifier.UnreadChangedFor);
    }

    [Fact]
    public async Task SendMessage_BetweenNonFriends_DoesNothing()
    {
        using var context = NewContext();
        var (service, notifier) = NewService(context, areFriends: false);

        var message = await service.SendMessageAsync("alice", "stranger", "Hi");

        Assert.Null(message);
        Assert.Empty(context.DirectMessages);
        Assert.Empty(context.Conversations);
        Assert.Empty(notifier.MessagesSentTo);
    }

    [Fact]
    public async Task SendMessage_WithEmptyContent_DoesNothing()
    {
        using var context = NewContext();
        var (service, _) = NewService(context);

        var message = await service.SendMessageAsync("alice", "bob", "   ");

        Assert.Null(message);
        Assert.Empty(context.DirectMessages);
    }

    [Fact]
    public async Task GetOrCreateConversation_ExistingPair_ReturnsSameConversation()
    {
        using var context = NewContext();
        var (service, _) = NewService(context);

        var first = await service.GetOrCreateConversationAsync("alice", "bob");
        var second = await service.GetOrCreateConversationAsync("alice", "bob");

        Assert.NotNull(first);
        Assert.Equal(first, second);
        Assert.Single(context.Conversations);
    }

    [Fact]
    public async Task GetOrCreateConversation_NormalizesParticipantOrder()
    {
        using var context = NewContext();
        var (service, _) = NewService(context);

        // Opened from opposite directions; must resolve to one conversation.
        var fromAlice = await service.GetOrCreateConversationAsync("alice", "bob");
        var fromBob = await service.GetOrCreateConversationAsync("bob", "alice");

        Assert.Equal(fromAlice, fromBob);
        var conversation = Assert.Single(context.Conversations);
        Assert.Equal("alice", conversation.User1Id); // "alice" < "bob" ordinally
        Assert.Equal("bob", conversation.User2Id);
    }

    [Fact]
    public async Task GetOrCreateConversation_BetweenNonFriends_ReturnsNull()
    {
        using var context = NewContext();
        var (service, _) = NewService(context, areFriends: false);

        var conversationId = await service.GetOrCreateConversationAsync("alice", "stranger");

        Assert.Null(conversationId);
        Assert.Empty(context.Conversations);
    }

    [Fact]
    public async Task GetThread_ForNonParticipant_ReturnsEmpty()
    {
        using var context = NewContext();
        var (service, _) = NewService(context);
        await service.SendMessageAsync("alice", "bob", "secret");
        var conversationId = (await service.GetOrCreateConversationAsync("alice", "bob"))!.Value;

        var thread = await service.GetThreadAsync("eve", conversationId);

        Assert.Empty(thread);
    }

    [Fact]
    public async Task GetThread_ForParticipant_ReturnsMessagesOldestFirst()
    {
        using var context = NewContext();
        var (service, _) = NewService(context);
        await service.SendMessageAsync("alice", "bob", "first");
        await service.SendMessageAsync("bob", "alice", "second");
        var conversationId = (await service.GetOrCreateConversationAsync("alice", "bob"))!.Value;

        var thread = await service.GetThreadAsync("alice", conversationId);

        Assert.Equal(2, thread.Count);
        Assert.Equal("first", thread[0].Content);
        Assert.Equal("second", thread[1].Content);
    }

    [Fact]
    public async Task MarkRead_SetsReadAtOnOtherPartyMessagesOnly()
    {
        using var context = NewContext();
        var (service, _) = NewService(context);
        await service.SendMessageAsync("alice", "bob", "from alice");
        await service.SendMessageAsync("bob", "alice", "from bob");
        var conversationId = (await service.GetOrCreateConversationAsync("alice", "bob"))!.Value;

        await service.MarkReadAsync("bob", conversationId);

        var fromAlice = context.DirectMessages.Single(m => m.SenderId == "alice");
        var fromBob = context.DirectMessages.Single(m => m.SenderId == "bob");
        Assert.NotNull(fromAlice.ReadAt);  // bob read alice's message
        Assert.Null(fromBob.ReadAt);       // bob's own message stays unread
    }

    [Fact]
    public async Task GetUnreadCount_CountsOnlyIncomingUnread()
    {
        using var context = NewContext();
        var (service, _) = NewService(context);
        await service.SendMessageAsync("alice", "bob", "one");
        await service.SendMessageAsync("alice", "bob", "two");
        await service.SendMessageAsync("bob", "alice", "mine");

        var bobUnread = await service.GetUnreadCountAsync("bob");
        var aliceUnread = await service.GetUnreadCountAsync("alice");

        Assert.Equal(2, bobUnread);  // bob hasn't read alice's two messages
        Assert.Equal(1, aliceUnread); // alice hasn't read bob's one message
    }

    [Fact]
    public async Task GetUnreadCount_AfterMarkRead_DropsToZero()
    {
        using var context = NewContext();
        var (service, _) = NewService(context);
        await service.SendMessageAsync("alice", "bob", "one");
        var conversationId = (await service.GetOrCreateConversationAsync("alice", "bob"))!.Value;

        await service.MarkReadAsync("bob", conversationId);

        Assert.Equal(0, await service.GetUnreadCountAsync("bob"));
    }

    // --- Test doubles -------------------------------------------------------

    private sealed class StubFriendshipChecker : IFriendshipChecker
    {
        private readonly bool _areFriends;

        public StubFriendshipChecker(bool areFriends) => _areFriends = areFriends;

        public Task<bool> AreFriendsAsync(string userIdA, string userIdB) =>
            Task.FromResult(_areFriends);
    }

    private sealed class RecordingNotifier : IMessageNotifier
    {
        public List<string> MessagesSentTo { get; } = new();
        public List<string> UnreadChangedFor { get; } = new();

        public Task MessageSentAsync(string recipientId, MessageDto message)
        {
            MessagesSentTo.Add(recipientId);
            return Task.CompletedTask;
        }

        public Task UnreadCountChangedAsync(string userId, int unreadCount)
        {
            UnreadChangedFor.Add(userId);
            return Task.CompletedTask;
        }
    }
}
