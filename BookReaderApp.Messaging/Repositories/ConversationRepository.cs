using BookReaderApp.Messaging.Data;
using BookReaderApp.Messaging.Models;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Messaging.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly MessagingDbContext _context;

    public ConversationRepository(MessagingDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetByIdAsync(int id) =>
        await _context.Conversations.FindAsync(id);

    public async Task<Conversation?> GetBetweenAsync(string userAId, string userBId)
    {
        var (user1Id, user2Id) = Conversation.NormalizePair(userAId, userBId);
        return await _context.Conversations
            .FirstOrDefaultAsync(c => c.User1Id == user1Id && c.User2Id == user2Id);
    }

    public async Task<IReadOnlyList<Conversation>> GetForUserAsync(string userId) =>
        await _context.Conversations
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();

    public async Task AddAsync(Conversation conversation) =>
        await _context.Conversations.AddAsync(conversation);

    public void Update(Conversation conversation) =>
        _context.Conversations.Update(conversation);

    public Task<int> SaveChangesAsync() =>
        _context.SaveChangesAsync();
}
