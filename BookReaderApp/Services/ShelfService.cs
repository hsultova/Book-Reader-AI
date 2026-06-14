using BookReaderApp.Models;
using BookReaderApp.Repositories;

namespace BookReaderApp.Services;

public class ShelfService : IShelfService
{
    private readonly IShelfRepository _shelves;

    public ShelfService(IShelfRepository shelves)
    {
        _shelves = shelves;
    }

    public Task<IReadOnlyList<Shelf>> GetShelvesAsync(string userId) =>
        _shelves.GetForUserAsync(userId);

    public async Task<Shelf> GetOrCreateAsync(string userId, string name)
    {
        name = name.Trim();

        var existing = await _shelves.GetByNameAsync(userId, name);
        if (existing is not null)
        {
            return existing;
        }

        var shelf = new Shelf
        {
            UserId = userId,
            Name = name,
            CreatedAt = DateTime.UtcNow,
        };
        await _shelves.AddAsync(shelf);
        await _shelves.SaveChangesAsync();
        return shelf;
    }
}
