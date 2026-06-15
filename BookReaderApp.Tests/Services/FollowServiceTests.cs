using BookReaderApp.Data;
using BookReaderApp.Models;
using BookReaderApp.Repositories;
using BookReaderApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookReaderApp.Tests.Services;

public class FollowServiceTests
{
    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static FollowService NewService(ApplicationDbContext context) =>
        new(new FollowRepository(context), NullLogger<FollowService>.Instance);

    [Fact]
    public async Task FollowAsync_WhenNotFollowing_AddsFollow()
    {
        using var context = NewContext();
        var service = NewService(context);

        await service.FollowAsync("user-1", "user-2");

        var follow = Assert.Single(context.Follows);
        Assert.Equal("user-1", follow.FollowerId);
        Assert.Equal("user-2", follow.FolloweeId);
    }

    [Fact]
    public async Task FollowAsync_WhenAlreadyFollowing_DoesNotDuplicate()
    {
        using var context = NewContext();
        var service = NewService(context);

        await service.FollowAsync("user-1", "user-2");
        await service.FollowAsync("user-1", "user-2");

        Assert.Single(context.Follows);
    }

    [Fact]
    public async Task FollowAsync_WithSelf_DoesNothing()
    {
        using var context = NewContext();
        var service = NewService(context);

        await service.FollowAsync("user-1", "user-1");

        Assert.Empty(context.Follows);
    }

    [Fact]
    public async Task UnfollowAsync_WhenFollowing_RemovesFollow()
    {
        using var context = NewContext();
        var service = NewService(context);
        await service.FollowAsync("user-1", "user-2");

        await service.UnfollowAsync("user-1", "user-2");

        Assert.Empty(context.Follows);
    }

    [Fact]
    public async Task UnfollowAsync_WhenNotFollowing_DoesNothing()
    {
        using var context = NewContext();
        var service = NewService(context);

        await service.UnfollowAsync("user-1", "user-2");

        Assert.Empty(context.Follows);
    }

    [Fact]
    public async Task IsFollowingAsync_ReflectsDirectionalRelationship()
    {
        using var context = NewContext();
        var service = NewService(context);
        await service.FollowAsync("user-1", "user-2");

        Assert.True(await service.IsFollowingAsync("user-1", "user-2"));
        // Following is one-way: the reverse direction is not implied.
        Assert.False(await service.IsFollowingAsync("user-2", "user-1"));
    }

    [Fact]
    public async Task GetFolloweeIdsAsync_ReturnsOnlyFollowerUsersFollowees()
    {
        using var context = NewContext();
        var service = NewService(context);
        await service.FollowAsync("user-1", "user-2");
        await service.FollowAsync("user-1", "user-3");
        await service.FollowAsync("user-9", "user-4");

        var followees = await service.GetFolloweeIdsAsync("user-1");

        Assert.Equal(2, followees.Count);
        Assert.Contains("user-2", followees);
        Assert.Contains("user-3", followees);
        Assert.DoesNotContain("user-4", followees);
    }
}
