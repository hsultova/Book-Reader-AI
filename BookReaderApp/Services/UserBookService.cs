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
}
