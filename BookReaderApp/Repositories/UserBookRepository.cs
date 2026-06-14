using BookReaderApp.Data;
using BookReaderApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Repositories;

public class UserBookRepository : EfRepository<UserBook>, IUserBookRepository
{
    public UserBookRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<UserBook>> GetForUserAsync(string userId) =>
        await Set
            .Include(ub => ub.Book)
                .ThenInclude(b => b!.Author)
            .Include(ub => ub.Book)
                .ThenInclude(b => b!.Genre)
            .Include(ub => ub.Shelf)
            .Where(ub => ub.UserId == userId)
            .OrderByDescending(ub => ub.AddedAt)
            .ToListAsync();

    public async Task<UserBook?> GetForUserAndBookAsync(string userId, int bookId) =>
        await Set.FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BookId == bookId);

    public async Task<IReadOnlyList<UserBook>> GetForShelfAsync(string userId, int shelfId) =>
        await Set.Where(ub => ub.UserId == userId && ub.ShelfId == shelfId).ToListAsync();

    public async Task<IReadOnlyDictionary<int, RatingSummary>> GetRatingSummariesAsync(
        IEnumerable<int> bookIds)
    {
        var ids = bookIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, RatingSummary>();
        }

        var summaries = await Set
            .Where(ub => ub.Rating != null && ids.Contains(ub.BookId))
            .GroupBy(ub => ub.BookId)
            .Select(g => new { BookId = g.Key, Average = g.Average(x => (double)x.Rating!.Value), Count = g.Count() })
            .ToListAsync();

        return summaries.ToDictionary(s => s.BookId, s => new RatingSummary(s.Average, s.Count));
    }
}
