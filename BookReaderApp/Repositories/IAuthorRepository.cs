using BookReaderApp.Models;

namespace BookReaderApp.Repositories;

public interface IAuthorRepository : IRepository<Author>
{
    Task<IReadOnlyList<Author>> GetAllAsync();
}
