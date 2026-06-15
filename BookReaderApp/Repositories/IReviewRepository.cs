using BookReaderApp.Models;

namespace BookReaderApp.Repositories;

// Specialized repository for book reviews. Extends the generic CRUD with queries for the
// community review list (eagerly loads the author) and a user's own review of a book.
public interface IReviewRepository : IRepository<Review>
{
    // All reviews for a book, newest first, with the authoring user loaded.
    Task<IReadOnlyList<Review>> GetForBookAsync(int bookId);

    // The most recent reviews authored by any of the given users (e.g. a viewer's friends),
    // newest first, with the authoring user and reviewed book loaded. For the activity feed.
    Task<IReadOnlyList<Review>> GetRecentForUsersAsync(IReadOnlyCollection<string> userIds, int take);

    // The single review for a user/book pair (the unique index guarantees at most one), or null.
    Task<Review?> GetForUserAndBookAsync(string userId, int bookId);
}
