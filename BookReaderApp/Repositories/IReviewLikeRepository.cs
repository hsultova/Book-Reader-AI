using BookReaderApp.Models;

namespace BookReaderApp.Repositories;

// Specialized repository for review likes. Extends the generic CRUD with the lookups
// needed to render like buttons: the current user's like on a review, like counts per
// review, and which reviews the current user has already liked.
public interface IReviewLikeRepository : IRepository<ReviewLike>
{
    // The current user's like on a review, or null if they haven't liked it.
    Task<ReviewLike?> GetForReviewAndUserAsync(int reviewId, string userId);

    // Like count keyed by review id. Reviews with no likes are omitted.
    Task<IReadOnlyDictionary<int, int>> GetCountsForReviewsAsync(IEnumerable<int> reviewIds);

    // The subset of the given review ids that the user has liked.
    Task<IReadOnlySet<int>> GetLikedReviewIdsAsync(string userId, IEnumerable<int> reviewIds);
}
