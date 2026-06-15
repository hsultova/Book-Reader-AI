using BookReaderApp.Models.ViewModels;

namespace BookReaderApp.Services;

// Builds the home-page Updates feed: recent activity (reviews, shelf additions and ratings)
// from the current user's accepted friends, merged into one newest-first timeline.
public interface IUpdatesService
{
    Task<IReadOnlyList<UpdateItem>> GetFeedAsync(string currentUserId, int take = 20);
}
