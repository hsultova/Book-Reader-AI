using BookReaderApp.Models;

namespace BookReaderApp.Services;

// Business logic for one-way follows: following/unfollowing and resolving the follow
// state that drives the profile button and the home Updates feed.
public interface IFollowService
{
    // Follows followeeId. No-op on a self-follow or if the follow already exists.
    Task FollowAsync(string followerId, string followeeId);

    // Removes the follow from followerId to followeeId, if any.
    Task UnfollowAsync(string followerId, string followeeId);

    // Whether followerId currently follows followeeId.
    Task<bool> IsFollowingAsync(string followerId, string followeeId);

    // The user ids the current user follows.
    Task<IReadOnlyList<string>> GetFolloweeIdsAsync(string followerId);

    // The users the current user follows, newest follow first.
    Task<IReadOnlyList<ApplicationUser>> GetFolloweesAsync(string followerId);
}
