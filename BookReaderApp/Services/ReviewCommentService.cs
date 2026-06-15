using BookReaderApp.Models;
using BookReaderApp.Repositories;

namespace BookReaderApp.Services;

public class ReviewCommentService : IReviewCommentService
{
    private readonly IReviewCommentRepository _comments;
    private readonly IReviewRepository _reviews;

    public ReviewCommentService(IReviewCommentRepository comments, IReviewRepository reviews)
    {
        _comments = comments;
        _reviews = reviews;
    }

    public async Task AddCommentAsync(string userId, int reviewId, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Comment text cannot be empty.", nameof(text));
        }

        var review = await _reviews.GetByIdAsync(reviewId);

        // You can only comment on reviews written by other readers.
        if (review is null || review.UserId == userId)
        {
            return;
        }

        await _comments.AddAsync(new ReviewComment
        {
            ReviewId = reviewId,
            UserId = userId,
            Text = text.Trim(),
            CreatedAt = DateTime.UtcNow,
        });

        await _comments.SaveChangesAsync();
    }

    public async Task DeleteCommentAsync(string userId, int commentId)
    {
        var comment = await _comments.GetByIdAsync(commentId);
        if (comment is null || comment.UserId != userId)
        {
            return;
        }

        _comments.Remove(comment);
        await _comments.SaveChangesAsync();
    }

    public Task<IReadOnlyDictionary<int, IReadOnlyList<ReviewComment>>> GetCommentsForReviewsAsync(
        IEnumerable<int> reviewIds) =>
        _comments.GetForReviewsAsync(reviewIds);
}
