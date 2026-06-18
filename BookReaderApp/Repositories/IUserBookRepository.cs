using BookReaderApp.Models;

namespace BookReaderApp.Repositories;

// Specialized repository for a user's bookshelf. Extends the generic CRUD with a
// user-scoped query that eagerly loads the associated Book for the "My Books" view.
public interface IUserBookRepository : IRepository<UserBook>
{
    Task<IReadOnlyList<UserBook>> GetForUserAsync(string userId);

    // A user's most highly-rated books (Rating >= minRating), newest rating first, with the
    // Book, its Author and Genre loaded. Drives the "Because you enjoyed..." recommendations.
    Task<IReadOnlyList<UserBook>> GetHighRatedForUserAsync(string userId, int minRating, int take);

    // The single shelf entry for a user/book pair (the unique index guarantees at most one), or null.
    Task<UserBook?> GetForUserAndBookAsync(string userId, int bookId);

    // All of a user's entries currently placed on the given custom shelf.
    Task<IReadOnlyList<UserBook>> GetForShelfAsync(string userId, int shelfId);

    // The most recent shelf additions by any of the given users (e.g. a viewer's friends),
    // newest first by AddedAt, with the user, book and (custom) shelf loaded. For the feed.
    Task<IReadOnlyList<UserBook>> GetRecentShelfAddsForUsersAsync(
        IReadOnlyCollection<string> userIds, int take);

    // The most recent ratings by any of the given users, newest first by RatedAt, with the
    // user and book loaded. Only entries that currently carry a rating are returned.
    Task<IReadOnlyList<UserBook>> GetRecentRatingsForUsersAsync(
        IReadOnlyCollection<string> userIds, int take);

    // Average rating and rating count per book, across all users, for the given books.
    // Books with no ratings are omitted from the result.
    Task<IReadOnlyDictionary<int, RatingSummary>> GetRatingSummariesAsync(IEnumerable<int> bookIds);

    // How many users currently have the book on a shelf with each reading status, across all
    // users. Statuses with no users are omitted from the result.
    Task<IReadOnlyDictionary<ReadingStatus, int>> GetStatusCountsAsync(int bookId);

    // A sample of users (newest shelf addition first) who currently have the book on a shelf
    // with the given status. Drives the avatar stack on the book Details page.
    Task<IReadOnlyList<ReaderAvatar>> GetStatusReadersAsync(int bookId, ReadingStatus status, int take);

    // How many books the user marked Finished during the given calendar year. Drives the
    // annual reading challenge progress on the profile page.
    Task<int> CountFinishedInYearAsync(string userId, int year);
}
