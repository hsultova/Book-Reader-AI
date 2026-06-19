using BookReaderApp.Models;

namespace BookReaderApp.Services;

// A single pick returned by the recommendation engine: which candidate book to suggest and
// the personalized reason to show the reader.
public sealed record BookPick(int BookId, string Reason);

// The "ask the model" seam. Given the reader's taste (books they rated highly) and a pool of
// candidate catalog books, returns chosen picks with reasons. Implemented by the Gemini-backed
// engine; isolating it here keeps AiRecommendationService unit-testable without a live API.
public interface IBookRecommendationEngine
{
    Task<IReadOnlyList<BookPick>> SuggestAsync(
        IReadOnlyList<Book> likedBooks,
        IReadOnlyList<Book> candidates,
        int max,
        CancellationToken ct = default);
}
