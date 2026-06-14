using BookReaderApp.Models;

namespace BookReaderApp.Repositories;

// Specialized repository for a user's bookshelf. Extends the generic CRUD with a
// user-scoped query that eagerly loads the associated Book for the "My Books" view.
public interface IUserBookRepository : IRepository<UserBook>
{
    Task<IReadOnlyList<UserBook>> GetForUserAsync(string userId);

    // The single shelf entry for a user/book pair (the unique index guarantees at most one), or null.
    Task<UserBook?> GetForUserAndBookAsync(string userId, int bookId);

    // All of a user's entries currently placed on the given custom shelf.
    Task<IReadOnlyList<UserBook>> GetForShelfAsync(string userId, int shelfId);
}
