using BookReaderApp.Data;
using BookReaderApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Repositories;

public class ReviewCommentRepository : EfRepository<ReviewComment>, IReviewCommentRepository
{
    public ReviewCommentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<ReviewComment>>> GetForReviewsAsync(
        IEnumerable<int> reviewIds)
    {
        var ids = reviewIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, IReadOnlyList<ReviewComment>>();
        }

        var comments = await Set
            .Include(c => c.User)
            .Where(c => ids.Contains(c.ReviewId))
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return comments
            .GroupBy(c => c.ReviewId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ReviewComment>)g.ToList());
    }
}
