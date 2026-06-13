using BookReaderApp.Models;

namespace BookReaderApp.Services;

public interface IGenreService
{
    Task<IReadOnlyList<Genre>> GetAllGenresAsync();
    Task<Genre?> GetGenreByIdAsync(int id);
}
