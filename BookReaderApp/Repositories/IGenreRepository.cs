using BookReaderApp.Models;

namespace BookReaderApp.Repositories;

public interface IGenreRepository : IRepository<Genre>
{
    Task<IReadOnlyList<Genre>> GetAllAsync();
}
