using BookReaderApp.Models;

namespace BookReaderApp.Repositories;

public interface IGenreRepository : IRepository<Genre>
{
    Task<IReadOnlyList<Genre>> GetAllAsync();

    // Looks up a genre by name (trimmed, case-insensitive) so callers can reuse an
    // existing genre instead of inserting a duplicate. Returns null when none matches.
    Task<Genre?> GetByNameAsync(string name);

    // Loads a genre together with its books (and each book's author) for the genre
    // browse page. Returns null when no genre has that id.
    Task<Genre?> GetWithBooksAsync(int id);
}
