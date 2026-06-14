using BookReaderApp.Data;
using BookReaderApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Repositories;

public class ShelfRepository : EfRepository<Shelf>, IShelfRepository
{
    public ShelfRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Shelf>> GetForUserAsync(string userId) =>
        await Set
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.Name)
            .ToListAsync();

    public async Task<Shelf?> GetByNameAsync(string userId, string name) =>
        await Set.FirstOrDefaultAsync(s =>
            s.UserId == userId && s.Name.ToLower() == name.ToLower());
}
