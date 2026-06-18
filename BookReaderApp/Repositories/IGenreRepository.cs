using BookReaderApp.Models;

namespace BookReaderApp.Repositories;

public interface IGenreRepository : IRepository<Genre>
{
    Task<IReadOnlyList<Genre>> GetAllAsync();

    // Looks up a genre by name (trimmed, case-insensitive) so callers can reuse an
    // existing genre instead of inserting a duplicate. Returns null when none matches.
    Task<Genre?> GetByNameAsync(string name);
}
