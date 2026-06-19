using BookReaderApp.Data;
using BookReaderApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Repositories;

public class GenreRepository : EfRepository<Genre>, IGenreRepository
{
    public GenreRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Genre>> GetAllAsync() =>
        await Set.OrderBy(g => g.Name).ToListAsync();

    public async Task<Genre?> GetByNameAsync(string name)
    {
        var normalized = name.Trim().ToLower();
        return await Set.FirstOrDefaultAsync(g => g.Name.ToLower() == normalized);
    }

    public async Task<Genre?> GetWithBooksAsync(int id) =>
        await Set
            .Include(g => g.Books)
                .ThenInclude(b => b.Author)
            .FirstOrDefaultAsync(g => g.Id == id);
}
