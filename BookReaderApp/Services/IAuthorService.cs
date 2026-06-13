using BookReaderApp.Models;

namespace BookReaderApp.Services;

public interface IAuthorService
{
    Task<IReadOnlyList<Author>> GetAllAuthorsAsync();
    Task<Author?> GetAuthorByIdAsync(int id);
}
