using BookReaderApp.Models;
using BookReaderApp.Repositories;

namespace BookReaderApp.Services;

public class UserBookService : IUserBookService
{
    private readonly IUserBookRepository _userBooks;

    public UserBookService(IUserBookRepository userBooks)
    {
        _userBooks = userBooks;
    }

    public Task<IReadOnlyList<UserBook>> GetMyBooksAsync(string userId) =>
        _userBooks.GetForUserAsync(userId);

    public async Task SetStatusAsync(string userId, int bookId, ReadingStatus status)
    {
        var existing = await _userBooks.GetForUserAndBookAsync(userId, bookId);
        if (existing is null)
        {
            await _userBooks.AddAsync(new UserBook
            {
                UserId = userId,
                BookId = bookId,
                Status = status,
                AddedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.Status = status;
            existing.ShelfId = null;
            _userBooks.Update(existing);
        }

        await _userBooks.SaveChangesAsync();
    }

    public async Task SetShelfAsync(string userId, int bookId, int shelfId)
    {
        var existing = await _userBooks.GetForUserAndBookAsync(userId, bookId);
        if (existing is null)
        {
            await _userBooks.AddAsync(new UserBook
            {
                UserId = userId,
                BookId = bookId,
                ShelfId = shelfId,
                AddedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.ShelfId = shelfId;
            existing.Status = null;
            _userBooks.Update(existing);
        }

        await _userBooks.SaveChangesAsync();
    }

    public async Task RemoveAsync(string userId, int bookId)
    {
        var existing = await _userBooks.GetForUserAndBookAsync(userId, bookId);
        if (existing is null)
        {
            return;
        }

        _userBooks.Remove(existing);
        await _userBooks.SaveChangesAsync();
    }

    public async Task SetRatingAsync(string userId, int bookId, int rating)
    {
        if (rating is < 0 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), rating, "Rating must be between 0 and 5.");
        }

        // 0 clears the rating; 1–5 sets it.
        int? value = rating == 0 ? null : rating;
        DateTime? ratedAt = value is null ? null : DateTime.UtcNow;

        var existing = await _userBooks.GetForUserAndBookAsync(userId, bookId);
        if (existing is null)
        {
            await _userBooks.AddAsync(new UserBook
            {
                UserId = userId,
                BookId = bookId,
                Status = ReadingStatus.WantToRead,
                Rating = value,
                RatedAt = ratedAt,
                AddedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.Rating = value;
            existing.RatedAt = ratedAt;
            _userBooks.Update(existing);
        }

        await _userBooks.SaveChangesAsync();
    }

    public Task<IReadOnlyDictionary<int, RatingSummary>> GetRatingSummariesAsync(IEnumerable<int> bookIds) =>
        _userBooks.GetRatingSummariesAsync(bookIds);
}
