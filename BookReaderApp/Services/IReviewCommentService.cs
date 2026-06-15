using BookReaderApp.Models;

namespace BookReaderApp.Services;

// Business logic for commenting on other readers' reviews.
public interface IReviewCommentService
{
    // Adds a comment to a review. Throws if the text is blank. No-op if the review doesn't
    // exist or the user authored it (you can't comment on your own review).
    Task AddCommentAsync(string userId, int reviewId, string text);

    // Deletes a comment, but only if the given user authored it (no-op otherwise).
    Task DeleteCommentAsync(string userId, int commentId);

    // Comments keyed by review id, each list oldest-first, with the commenting user loaded.
    // Reviews with no comments are omitted.
    Task<IReadOnlyDictionary<int, IReadOnlyList<ReviewComment>>> GetCommentsForReviewsAsync(
        IEnumerable<int> reviewIds);
}
