using BookReaderApp.Models;
using BookReaderApp.Repositories;

namespace BookReaderApp.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviews;

    public ReviewService(IReviewRepository reviews)
    {
        _reviews = reviews;
    }

    public Task<IReadOnlyList<Review>> GetForBookAsync(int bookId) =>
        _reviews.GetForBookAsync(bookId);

    public Task<Review?> GetUserReviewAsync(string userId, int bookId) =>
        _reviews.GetForUserAndBookAsync(userId, bookId);

    public async Task SaveReviewAsync(string userId, int bookId, string text, bool containsSpoilers)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Review text cannot be empty.", nameof(text));
        }

        var trimmed = text.Trim();
        var now = DateTime.UtcNow;

        var existing = await _reviews.GetForUserAndBookAsync(userId, bookId);
        if (existing is null)
        {
            await _reviews.AddAsync(new Review
            {
                UserId = userId,
                BookId = bookId,
                Text = trimmed,
                ContainsSpoilers = containsSpoilers,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
        else
        {
            existing.Text = trimmed;
            existing.ContainsSpoilers = containsSpoilers;
            existing.UpdatedAt = now;
            _reviews.Update(existing);
        }

        await _reviews.SaveChangesAsync();
    }

    public async Task DeleteReviewAsync(string userId, int bookId)
    {
        var existing = await _reviews.GetForUserAndBookAsync(userId, bookId);
        if (existing is null)
        {
            return;
        }

        _reviews.Remove(existing);
        await _reviews.SaveChangesAsync();
    }
}
