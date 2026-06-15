using BookReaderApp.Models;
using BookReaderApp.Repositories;

namespace BookReaderApp.Services;

public class ReviewLikeService : IReviewLikeService
{
    private readonly IReviewLikeRepository _likes;
    private readonly IReviewRepository _reviews;

    public ReviewLikeService(IReviewLikeRepository likes, IReviewRepository reviews)
    {
        _likes = likes;
        _reviews = reviews;
    }

    public async Task ToggleLikeAsync(string userId, int reviewId)
    {
        var review = await _reviews.GetByIdAsync(reviewId);

        // You can only like reviews written by other readers.
        if (review is null || review.UserId == userId)
        {
            return;
        }

        var existing = await _likes.GetForReviewAndUserAsync(reviewId, userId);
        if (existing is null)
        {
            await _likes.AddAsync(new ReviewLike
            {
                ReviewId = reviewId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            _likes.Remove(existing);
        }

        await _likes.SaveChangesAsync();
    }

    public Task<IReadOnlyDictionary<int, int>> GetLikeCountsAsync(IEnumerable<int> reviewIds) =>
        _likes.GetCountsForReviewsAsync(reviewIds);

    public Task<IReadOnlySet<int>> GetLikedReviewIdsAsync(string userId, IEnumerable<int> reviewIds) =>
        _likes.GetLikedReviewIdsAsync(userId, reviewIds);
}
