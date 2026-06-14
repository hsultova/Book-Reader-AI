using BookReaderApp.Models;

namespace BookReaderApp.Services;

// Business logic for a user's personal bookshelf ("My Books").
public interface IUserBookService
{
    Task<IReadOnlyList<UserBook>> GetMyBooksAsync(string userId);

    // Adds the book to the user's shelf with the given status, or updates the status if already shelved.
    // Clears any custom-shelf placement (one shelf per book).
    Task SetStatusAsync(string userId, int bookId, ReadingStatus status);

    // Places the book on the given custom shelf, clearing any built-in status (one shelf per book).
    Task SetShelfAsync(string userId, int bookId, int shelfId);

    // Removes the book from the user's shelf (no-op if it isn't shelved).
    Task RemoveAsync(string userId, int bookId);
}
