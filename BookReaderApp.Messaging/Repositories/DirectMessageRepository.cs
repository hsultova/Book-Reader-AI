using BookReaderApp.Messaging.Data;
using BookReaderApp.Messaging.Models;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Messaging.Repositories;

public class DirectMessageRepository : IDirectMessageRepository
{
    private readonly MessagingDbContext _context;

    public DirectMessageRepository(MessagingDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(DirectMessage message) =>
        await _context.DirectMessages.AddAsync(message);

    public async Task<IReadOnlyList<DirectMessage>> GetThreadAsync(int conversationId, int take)
    {
        // Take the newest `take` rows, then flip to chronological order for display.
        var newest = await _context.DirectMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Take(take)
            .ToListAsync();

        newest.Reverse();
        return newest;
    }

    public async Task<DirectMessage?> GetLastForConversationAsync(int conversationId) =>
        await _context.DirectMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .FirstOrDefaultAsync();

    public async Task<int> CountUnreadInConversationAsync(int conversationId, string userId) =>
        await _context.DirectMessages
            .Where(m => m.ConversationId == conversationId
                        && m.SenderId != userId
                        && m.ReadAt == null)
            .CountAsync();

    public async Task<int> CountUnreadForUserAsync(string userId) =>
        await _context.DirectMessages
            .Where(m => m.ReadAt == null
                        && m.SenderId != userId
                        && (m.Conversation!.User1Id == userId || m.Conversation.User2Id == userId))
            .CountAsync();

    public async Task<int> MarkReadAsync(int conversationId, string readerUserId)
    {
        var unread = await _context.DirectMessages
            .Where(m => m.ConversationId == conversationId
                        && m.SenderId != readerUserId
                        && m.ReadAt == null)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var message in unread)
        {
            message.ReadAt = now;
        }

        return unread.Count;
    }

    public Task<int> SaveChangesAsync() =>
        _context.SaveChangesAsync();
}
