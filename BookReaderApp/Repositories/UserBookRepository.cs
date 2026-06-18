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

    public async Task<IReadOnlyList<UserBook>> GetHighRatedForUserAsync(
        string userId, int minRating, int take) =>
        await Set
            .Include(ub => ub.Book)
                .ThenInclude(b => b!.Author)
            .Include(ub => ub.Book)
                .ThenInclude(b => b!.Genre)
            .Where(ub => ub.UserId == userId && ub.Rating != null && ub.Rating >= minRating)
            .OrderByDescending(ub => ub.RatedAt)
            .Take(take)
            .ToListAsync();

    public async Task<UserBook?> GetForUserAndBookAsync(string userId, int bookId) =>
        await Set.FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BookId == bookId);

    public async Task<IReadOnlyList<UserBook>> GetForShelfAsync(string userId, int shelfId) =>
        await Set.Where(ub => ub.UserId == userId && ub.ShelfId == shelfId).ToListAsync();

    public async Task<IReadOnlyList<UserBook>> GetRecentShelfAddsForUsersAsync(
        IReadOnlyCollection<string> userIds, int take) =>
        await Set
            .Include(ub => ub.User)
            .Include(ub => ub.Book)
            .Include(ub => ub.Shelf)
            .Where(ub => userIds.Contains(ub.UserId))
            .OrderByDescending(ub => ub.AddedAt)
            .Take(take)
            .ToListAsync();

    public async Task<IReadOnlyList<UserBook>> GetRecentRatingsForUsersAsync(
        IReadOnlyCollection<string> userIds, int take) =>
        await Set
            .Include(ub => ub.User)
            .Include(ub => ub.Book)
            .Where(ub => ub.Rating != null && ub.RatedAt != null && userIds.Contains(ub.UserId))
            .OrderByDescending(ub => ub.RatedAt)
            .Take(take)
            .ToListAsync();

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

    public async Task<IReadOnlyDictionary<ReadingStatus, int>> GetStatusCountsAsync(int bookId)
    {
        var counts = await Set
            .Where(ub => ub.BookId == bookId && ub.Status != null)
            .GroupBy(ub => ub.Status!.Value)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return counts.ToDictionary(c => c.Status, c => c.Count);
    }

    public async Task<IReadOnlyList<ReaderAvatar>> GetStatusReadersAsync(
        int bookId, ReadingStatus status, int take) =>
        await Set
            .Where(ub => ub.BookId == bookId && ub.Status == status)
            .OrderByDescending(ub => ub.AddedAt)
            .Take(take)
            .Select(ub => new ReaderAvatar(ub.UserId, ub.User!.DisplayName, ub.User.ProfilePicturePath))
            .ToListAsync();

    public async Task<int> CountFinishedInYearAsync(string userId, int year) =>
        await Set.CountAsync(ub =>
            ub.UserId == userId
            && ub.Status == ReadingStatus.Finished
            && ub.FinishedAt != null
            && ub.FinishedAt.Value.Year == year);
}
