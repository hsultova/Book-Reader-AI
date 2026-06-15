using BookReaderApp.Data;
using BookReaderApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Repositories;

public class ReviewRepository : EfRepository<Review>, IReviewRepository
{
    public ReviewRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Review>> GetForBookAsync(int bookId) =>
        await Set
            .Include(r => r.User)
            .Where(r => r.BookId == bookId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<Review>> GetRecentForUsersAsync(
        IReadOnlyCollection<string> userIds, int take) =>
        await Set
            .Include(r => r.User)
            .Include(r => r.Book)
            .Where(r => userIds.Contains(r.UserId))
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .ToListAsync();

    public async Task<Review?> GetForUserAndBookAsync(string userId, int bookId) =>
        await Set.FirstOrDefaultAsync(r => r.UserId == userId && r.BookId == bookId);
}
