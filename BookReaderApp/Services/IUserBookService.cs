using BookReaderApp.Models;

namespace BookReaderApp.Services;

// Business logic for a user's personal bookshelf ("My Books").
public interface IUserBookService
{
    Task<IReadOnlyList<UserBook>> GetMyBooksAsync(string userId);
}
