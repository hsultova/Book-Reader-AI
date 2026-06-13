using BookReaderApp.Models;
using BookReaderApp.Repositories;

namespace BookReaderApp.Services;

public class GenreService : IGenreService
{
    private readonly IGenreRepository _genres;

    public GenreService(IGenreRepository genres)
    {
        _genres = genres;
    }

    public Task<IReadOnlyList<Genre>> GetAllGenresAsync() =>
        _genres.GetAllAsync();

    public Task<Genre?> GetGenreByIdAsync(int id) =>
        _genres.GetByIdAsync(id);
}
