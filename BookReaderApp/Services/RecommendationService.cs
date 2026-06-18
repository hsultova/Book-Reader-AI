using BookReaderApp.Models.ViewModels;
using BookReaderApp.Repositories;

namespace BookReaderApp.Services;

// Derives recommendations from a user's high ratings: for each recently 4-5 star book we
// suggest other books by the same author or in the same genre that the user doesn't already
// have. No DbContext access — works through the repositories like the other services.
public class RecommendationService : IRecommendationService
{
    // Only 4-5 star ratings signal genuine enjoyment.
    private const int MinRating = 4;
    // How many "Because you enjoyed..." rows to show, newest rating first.
    private const int MaxGroups = 3;
    // Books per row.
    private const int BooksPerGroup = 6;

    private readonly IUserBookRepository _userBooks;
    private readonly IBookRepository _books;

    public RecommendationService(IUserBookRepository userBooks, IBookRepository books)
    {
        _userBooks = userBooks;
        _books = books;
    }

    public async Task<IReadOnlyList<RecommendationGroupViewModel>> GetRecommendationsAsync(string userId)
    {
        var highRated = await _userBooks.GetHighRatedForUserAsync(userId, MinRating, MaxGroups);
        if (highRated.Count == 0)
        {
            return Array.Empty<RecommendationGroupViewModel>();
        }

        // Exclude everything already on the user's shelves (this also covers the source books),
        // and keep extending it so a book never appears in more than one row.
        var owned = await _userBooks.GetForUserAsync(userId);
        var excluded = new HashSet<int>(owned.Select(ub => ub.BookId));

        var groups = new List<RecommendationGroupViewModel>();
        foreach (var entry in highRated)
        {
            var source = entry.Book;
            if (source is null)
            {
                continue;
            }

            var similar = await _books.GetSimilarAsync(
                source.AuthorId, source.GenreId, excluded, BooksPerGroup);
            if (similar.Count == 0)
            {
                continue;
            }

            groups.Add(new RecommendationGroupViewModel(
                $"Because you enjoyed \"{source.Title}\"", similar));
            foreach (var book in similar)
            {
                excluded.Add(book.Id);
            }
        }

        return groups;
    }
}
