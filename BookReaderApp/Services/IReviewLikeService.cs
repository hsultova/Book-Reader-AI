using BookReaderApp.Models;

namespace BookReaderApp.Services;

// Business logic for liking other readers' reviews.
public interface IReviewLikeService
{
    // Toggles the user's like on a review: likes it if not yet liked, otherwise removes
    // the like. No-op if the review doesn't exist or the user authored it (you can't like
    // your own review).
    Task ToggleLikeAsync(string userId, int reviewId);

    // Like count keyed by review id. Reviews with no likes are omitted.
    Task<IReadOnlyDictionary<int, int>> GetLikeCountsAsync(IEnumerable<int> reviewIds);

    // The subset of the given review ids that the user has liked.
    Task<IReadOnlySet<int>> GetLikedReviewIdsAsync(string userId, IEnumerable<int> reviewIds);
}
