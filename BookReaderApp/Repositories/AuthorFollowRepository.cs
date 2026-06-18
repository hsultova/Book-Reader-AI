using BookReaderApp.Data;
using BookReaderApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Repositories;

public class AuthorFollowRepository : EfRepository<AuthorFollow>, IAuthorFollowRepository
{
    public AuthorFollowRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<AuthorFollow?> GetAsync(string userId, int authorId) =>
        await Set.FirstOrDefaultAsync(f => f.UserId == userId && f.AuthorId == authorId);

    public async Task<bool> IsFollowingAsync(string userId, int authorId) =>
        await Set.AnyAsync(f => f.UserId == userId && f.AuthorId == authorId);

    public async Task<int> GetFollowerCountAsync(int authorId) =>
        await Set.CountAsync(f => f.AuthorId == authorId);
}
