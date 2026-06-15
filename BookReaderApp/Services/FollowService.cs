using BookReaderApp.Models;
using BookReaderApp.Repositories;

namespace BookReaderApp.Services;

public class FollowService : IFollowService
{
    private readonly IFollowRepository _follows;
    private readonly ILogger<FollowService> _logger;

    public FollowService(IFollowRepository follows, ILogger<FollowService> logger)
    {
        _follows = follows;
        _logger = logger;
    }

    public async Task FollowAsync(string followerId, string followeeId)
    {
        if (followerId == followeeId)
        {
            return;
        }

        // Idempotent: a duplicate follow would violate the unique index anyway.
        if (await _follows.IsFollowingAsync(followerId, followeeId))
        {
            return;
        }

        await _follows.AddAsync(new Follow
        {
            FollowerId = followerId,
            FolloweeId = followeeId,
            CreatedAt = DateTime.UtcNow
        });
        await _follows.SaveChangesAsync();
        _logger.LogInformation("{Follower} started following {Followee}.", followerId, followeeId);
    }

    public async Task UnfollowAsync(string followerId, string followeeId)
    {
        var follow = await _follows.GetAsync(followerId, followeeId);
        if (follow is null)
        {
            return;
        }

        _follows.Remove(follow);
        await _follows.SaveChangesAsync();
        _logger.LogInformation("{Follower} unfollowed {Followee}.", followerId, followeeId);
    }

    public Task<bool> IsFollowingAsync(string followerId, string followeeId) =>
        _follows.IsFollowingAsync(followerId, followeeId);

    public Task<IReadOnlyList<string>> GetFolloweeIdsAsync(string followerId) =>
        _follows.GetFolloweeIdsAsync(followerId);

    public async Task<IReadOnlyList<ApplicationUser>> GetFolloweesAsync(string followerId)
    {
        var follows = await _follows.GetFolloweesAsync(followerId);
        return follows
            .Where(f => f.Followee is not null)
            .Select(f => f.Followee!)
            .ToList();
    }
}
