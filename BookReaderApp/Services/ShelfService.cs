using BookReaderApp.Models;
using BookReaderApp.Repositories;

namespace BookReaderApp.Services;

public class ShelfService : IShelfService
{
    private readonly IShelfRepository _shelves;
    private readonly IUserBookRepository _userBooks;

    public ShelfService(IShelfRepository shelves, IUserBookRepository userBooks)
    {
        _shelves = shelves;
        _userBooks = userBooks;
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

    public async Task DeleteAsync(string userId, int shelfId)
    {
        var shelf = await _shelves.GetByIdAsync(shelfId);
        if (shelf is null || shelf.UserId != userId)
        {
            return;
        }

        // Books on this shelf fall back to the "Want to Read" built-in shelf.
        var books = await _userBooks.GetForShelfAsync(userId, shelfId);
        foreach (var book in books)
        {
            book.ShelfId = null;
            book.Status = ReadingStatus.WantToRead;
            _userBooks.Update(book);
        }

        _shelves.Remove(shelf);
        await _shelves.SaveChangesAsync();
    }
}
