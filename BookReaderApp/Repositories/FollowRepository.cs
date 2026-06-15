using BookReaderApp.Data;
using BookReaderApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Repositories;

public class FollowRepository : EfRepository<Follow>, IFollowRepository
{
    public FollowRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Follow?> GetAsync(string followerId, string followeeId) =>
        await Set.FirstOrDefaultAsync(f =>
            f.FollowerId == followerId && f.FolloweeId == followeeId);

    public async Task<bool> IsFollowingAsync(string followerId, string followeeId) =>
        await Set.AnyAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId);

    public async Task<IReadOnlyList<string>> GetFolloweeIdsAsync(string followerId) =>
        await Set
            .Where(f => f.FollowerId == followerId)
            .Select(f => f.FolloweeId)
            .ToListAsync();

    public async Task<IReadOnlyList<Follow>> GetFolloweesAsync(string followerId) =>
        await Set
            .Include(f => f.Followee)
            .Where(f => f.FollowerId == followerId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
}
