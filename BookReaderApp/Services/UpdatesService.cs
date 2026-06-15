using BookReaderApp.Models;
using BookReaderApp.Models.ViewModels;
using BookReaderApp.Repositories;

namespace BookReaderApp.Services;

public class UpdatesService : IUpdatesService
{
    // How many characters of a review body to surface inline before truncating.
    private const int SnippetLength = 160;

    private readonly IFriendRequestService _friends;
    private readonly IReviewRepository _reviews;
    private readonly IUserBookRepository _userBooks;

    public UpdatesService(
        IFriendRequestService friends,
        IReviewRepository reviews,
        IUserBookRepository userBooks)
    {
        _friends = friends;
        _reviews = reviews;
        _userBooks = userBooks;
    }

    public async Task<IReadOnlyList<UpdateItem>> GetFeedAsync(string currentUserId, int take = 20)
    {
        var friendIds = await _friends.GetFriendIdsAsync(currentUserId);
        if (friendIds.Count == 0)
        {
            return [];
        }

        // Pull the latest `take` of each activity type, then merge: the global newest `take`
        // can't include anything older than the oldest of any single type's top `take`.
        var reviews = await _reviews.GetRecentForUsersAsync(friendIds, take);
        var shelfAdds = await _userBooks.GetRecentShelfAddsForUsersAsync(friendIds, take);
        var ratings = await _userBooks.GetRecentRatingsForUsersAsync(friendIds, take);

        return reviews.Select(ToReviewItem)
            .Concat(shelfAdds.Select(ToShelfAddItem))
            .Concat(ratings.Select(ToRatingItem))
            .OrderByDescending(u => u.Timestamp)
            .Take(take)
            .ToList();
    }

    private static UpdateItem ToReviewItem(Review r) =>
        new(
            UpdateKind.Review,
            r.User?.DisplayName ?? "A friend",
            r.User?.ProfilePicturePath,
            r.BookId,
            r.Book?.Title ?? "a book",
            r.CreatedAt,
            ShelfLabel: null,
            Rating: null,
            ReviewSnippet: Truncate(r.Text),
            ContainsSpoilers: r.ContainsSpoilers);

    private static UpdateItem ToShelfAddItem(UserBook ub) =>
        new(
            UpdateKind.ShelfAdd,
            ub.User?.DisplayName ?? "A friend",
            ub.User?.ProfilePicturePath,
            ub.BookId,
            ub.Book?.Title ?? "a book",
            ub.AddedAt,
            ShelfLabel: ub.Shelf?.Name ?? ub.Status?.DisplayName() ?? "their shelf",
            Rating: null,
            ReviewSnippet: null,
            ContainsSpoilers: false);

    private static UpdateItem ToRatingItem(UserBook ub) =>
        new(
            UpdateKind.Rating,
            ub.User?.DisplayName ?? "A friend",
            ub.User?.ProfilePicturePath,
            ub.BookId,
            ub.Book?.Title ?? "a book",
            ub.RatedAt!.Value,
            ShelfLabel: null,
            Rating: ub.Rating,
            ReviewSnippet: null,
            ContainsSpoilers: false);

    private static string Truncate(string text)
    {
        text = text.Trim();
        return text.Length <= SnippetLength ? text : text[..SnippetLength].TrimEnd() + "…";
    }
}
