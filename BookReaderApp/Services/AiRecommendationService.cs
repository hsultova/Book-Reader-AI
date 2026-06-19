using BookReaderApp.Configuration;
using BookReaderApp.Models;
using BookReaderApp.Models.ViewModels;
using BookReaderApp.Repositories;
using Microsoft.Extensions.Options;

namespace BookReaderApp.Services;

// Orchestrates AI recommendations without touching the LLM directly: it assembles the taste
// profile and a candidate pool from the repositories, hands them to the recommendation engine
// (the Gemini seam), then maps the engine's picks back onto real catalog books. The model is
// only ever asked to choose from candidate ids, and we additionally filter its answer against
// the candidate set here — a returned id we never offered is dropped.
public class AiRecommendationService : IAiRecommendationService
{
    // Only 4-5 star ratings signal genuine enjoyment (matches the rule-based service).
    private const int MinRating = 4;
    // How many highly-rated books to seed the taste profile and candidate search from.
    private const int LikedSeedCount = 5;
    // Candidate books gathered per seed book before deduplication.
    private const int CandidatesPerSeed = 10;
    // Hard cap on the candidate pool sent to the model, to bound token usage.
    private const int MaxCandidates = 30;

    private readonly IUserBookRepository _userBooks;
    private readonly IBookRepository _books;
    private readonly IBookRecommendationEngine _engine;
    private readonly int _maxRecommendations;

    public AiRecommendationService(
        IUserBookRepository userBooks,
        IBookRepository books,
        IBookRecommendationEngine engine,
        IOptions<GeminiOptions> options)
    {
        _userBooks = userBooks;
        _books = books;
        _engine = engine;
        _maxRecommendations = Math.Max(1, options.Value.MaxRecommendations);
    }

    public async Task<IReadOnlyList<AiRecommendationViewModel>> GetAiRecommendationsAsync(
        string userId, CancellationToken ct = default)
    {
        var likedBooks = (await _userBooks.GetHighRatedForUserAsync(userId, MinRating, LikedSeedCount))
            .Select(ub => ub.Book)
            .OfType<Book>()
            .ToList();

        // Without any taste signal there is nothing to personalize against.
        if (likedBooks.Count == 0)
        {
            return Array.Empty<AiRecommendationViewModel>();
        }

        var candidates = await BuildCandidatePoolAsync(userId, likedBooks);
        if (candidates.Count == 0)
        {
            return Array.Empty<AiRecommendationViewModel>();
        }

        var byId = candidates.ToDictionary(b => b.Id);
        var picks = await _engine.SuggestAsync(likedBooks, candidates, _maxRecommendations, ct);

        var seen = new HashSet<int>();
        var results = new List<AiRecommendationViewModel>();
        foreach (var pick in picks)
        {
            // Defensive: only accept ids we actually offered, and never the same book twice.
            if (byId.TryGetValue(pick.BookId, out var book) && seen.Add(book.Id))
            {
                results.Add(new AiRecommendationViewModel(book, pick.Reason));
                if (results.Count == _maxRecommendations)
                {
                    break;
                }
            }
        }

        return results;
    }

    // Gathers similar books (same author or genre) across the reader's highly-rated books,
    // excluding everything already on their shelves and deduplicating across seeds.
    private async Task<IReadOnlyList<Book>> BuildCandidatePoolAsync(
        string userId, IReadOnlyList<Book> likedBooks)
    {
        var owned = await _userBooks.GetForUserAsync(userId);
        var excluded = new HashSet<int>(owned.Select(ub => ub.BookId));

        var pool = new List<Book>();
        foreach (var seed in likedBooks)
        {
            var similar = await _books.GetSimilarAsync(
                seed.AuthorId, seed.GenreId, excluded, CandidatesPerSeed);
            foreach (var book in similar)
            {
                if (excluded.Add(book.Id))
                {
                    pool.Add(book);
                    if (pool.Count == MaxCandidates)
                    {
                        return pool;
                    }
                }
            }
        }

        return pool;
    }
}
