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

    // Sets the user's 1–5 star rating for the book; 0 clears the rating. Adds the book to the
    // user's shelf ("Want to Read") if it isn't already there. Throws if rating is outside 0–5.
    Task SetRatingAsync(string userId, int bookId, int rating);

    // Average rating and rating count per book, across all users, for the given books.
    Task<IReadOnlyDictionary<int, RatingSummary>> GetRatingSummariesAsync(IEnumerable<int> bookIds);

    // How many users currently have the book on a shelf with each reading status, across all
    // users. Statuses with no users are omitted from the result.
    Task<IReadOnlyDictionary<ReadingStatus, int>> GetStatusCountsAsync(int bookId);

    // A sample of users (newest shelf addition first) who currently have the book on a shelf
    // with the given status. Drives the avatar stack on the book Details page.
    Task<IReadOnlyList<ReaderAvatar>> GetStatusReadersAsync(int bookId, ReadingStatus status, int take);
}
