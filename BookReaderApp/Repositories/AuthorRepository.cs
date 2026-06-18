using BookReaderApp.Data;
using BookReaderApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Repositories;

public class AuthorRepository : EfRepository<Author>, IAuthorRepository
{
    public AuthorRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Author>> GetAllAsync() =>
        await Set.OrderBy(a => a.Name).ToListAsync();

    public async Task<Author?> GetWithBooksAsync(int id) =>
        await Set
            .Include(a => a.Books)
                .ThenInclude(b => b.Genre)
            .FirstOrDefaultAsync(a => a.Id == id);
}
