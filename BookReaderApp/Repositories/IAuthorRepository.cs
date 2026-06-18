using BookReaderApp.Models;

namespace BookReaderApp.Repositories;

public interface IAuthorRepository : IRepository<Author>
{
    Task<IReadOnlyList<Author>> GetAllAsync();

    // Returns the author with their books (and each book's genre) loaded.
    Task<Author?> GetWithBooksAsync(int id);
}
