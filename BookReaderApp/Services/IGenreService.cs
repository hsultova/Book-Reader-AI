using BookReaderApp.Models;

namespace BookReaderApp.Services;

public interface IGenreService
{
    Task<IReadOnlyList<Genre>> GetAllGenresAsync();
    Task<Genre?> GetGenreByIdAsync(int id);

    // Loads a genre together with its books for the genre browse page.
    Task<Genre?> GetGenreWithBooksAsync(int id);
}
