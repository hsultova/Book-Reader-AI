using BookReaderApp.Data;
using BookReaderApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Repositories;

public class UserBookRepository : EfRepository<UserBook>, IUserBookRepository
{
    public UserBookRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<UserBook>> GetForUserAsync(string userId) =>
        await Set
            .Include(ub => ub.Book)
            .Where(ub => ub.UserId == userId)
            .OrderByDescending(ub => ub.AddedAt)
            .ToListAsync();
}
