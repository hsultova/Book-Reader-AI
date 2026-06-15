using BookReaderApp.Models;

namespace BookReaderApp.Repositories;

// Specialized repository for follows. Extends generic CRUD with directional lookups
// used to resolve the follow button and to feed the home Updates page.
public interface IFollowRepository : IRepository<Follow>
{
    // The follow row from followerId to followeeId, or null.
    Task<Follow?> GetAsync(string followerId, string followeeId);

    // Whether followerId currently follows followeeId.
    Task<bool> IsFollowingAsync(string followerId, string followeeId);

    // The user ids that followerId follows.
    Task<IReadOnlyList<string>> GetFolloweeIdsAsync(string followerId);

    // The follow rows for everyone followerId follows, with the followed user loaded,
    // newest follow first.
    Task<IReadOnlyList<Follow>> GetFolloweesAsync(string followerId);
}
