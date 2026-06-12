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
}
