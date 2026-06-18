using BookReaderApp.Models;

namespace BookReaderApp.Repositories;

public interface IAuthorRepository : IRepository<Author>
{
    Task<IReadOnlyList<Author>> GetAllAsync();

    // Returns the author with their books (and each book's genre) loaded.
    Task<Author?> GetWithBooksAsync(int id);

    // Looks up an author by name (trimmed, case-insensitive) so callers can reuse an
    // existing author instead of inserting a duplicate. Returns null when none matches.
    Task<Author?> GetByNameAsync(string name);
}
