using BookReaderApp.Data;
using BookReaderApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Repositories;

public class ReviewLikeRepository : EfRepository<ReviewLike>, IReviewLikeRepository
{
    public ReviewLikeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<ReviewLike?> GetForReviewAndUserAsync(int reviewId, string userId) =>
        await Set.FirstOrDefaultAsync(l => l.ReviewId == reviewId && l.UserId == userId);

    public async Task<IReadOnlyDictionary<int, int>> GetCountsForReviewsAsync(IEnumerable<int> reviewIds)
    {
        var ids = reviewIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        var counts = await Set
            .Where(l => ids.Contains(l.ReviewId))
            .GroupBy(l => l.ReviewId)
            .Select(g => new { ReviewId = g.Key, Count = g.Count() })
            .ToListAsync();

        return counts.ToDictionary(c => c.ReviewId, c => c.Count);
    }

    public async Task<IReadOnlySet<int>> GetLikedReviewIdsAsync(string userId, IEnumerable<int> reviewIds)
    {
        var ids = reviewIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new HashSet<int>();
        }

        var liked = await Set
            .Where(l => l.UserId == userId && ids.Contains(l.ReviewId))
            .Select(l => l.ReviewId)
            .ToListAsync();

        return liked.ToHashSet();
    }
}
