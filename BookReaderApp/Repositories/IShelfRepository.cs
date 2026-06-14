using BookReaderApp.Models;

namespace BookReaderApp.Repositories;

// A user's custom shelves. Extends the generic CRUD with user-scoped queries.
public interface IShelfRepository : IRepository<Shelf>
{
    Task<IReadOnlyList<Shelf>> GetForUserAsync(string userId);

    // A user's shelf matching the given name (case-insensitive), or null.
    Task<Shelf?> GetByNameAsync(string userId, string name);
}
