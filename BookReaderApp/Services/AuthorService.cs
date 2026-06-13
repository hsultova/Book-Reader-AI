using BookReaderApp.Models;
using BookReaderApp.Repositories;

namespace BookReaderApp.Services;

public class AuthorService : IAuthorService
{
    private readonly IAuthorRepository _authors;

    public AuthorService(IAuthorRepository authors)
    {
        _authors = authors;
    }

    public Task<IReadOnlyList<Author>> GetAllAuthorsAsync() =>
        _authors.GetAllAsync();

    public Task<Author?> GetAuthorByIdAsync(int id) =>
        _authors.GetByIdAsync(id);
}
