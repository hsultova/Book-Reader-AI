using BookReaderApp.Models;

namespace BookReaderApp.Services;

// Business logic for a user's custom shelves.
public interface IShelfService
{
    Task<IReadOnlyList<Shelf>> GetShelvesAsync(string userId);

    // Returns the user's shelf with the given name, creating it if it doesn't exist.
    Task<Shelf> GetOrCreateAsync(string userId, string name);
}
