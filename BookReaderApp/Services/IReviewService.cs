using BookReaderApp.Models;

namespace BookReaderApp.Services;

// Business logic for book reviews.
public interface IReviewService
{
    // All reviews for a book, newest first, with the authoring user loaded.
    Task<IReadOnlyList<Review>> GetForBookAsync(int bookId);

    // The current user's own review of the book, or null if they haven't written one.
    Task<Review?> GetUserReviewAsync(string userId, int bookId);

    // Creates the user's review, or updates it if one already exists (one review per user per book).
    // Throws if the text is blank.
    Task SaveReviewAsync(string userId, int bookId, string text, bool containsSpoilers);

    // Deletes the user's review of the book (no-op if they haven't written one).
    Task DeleteReviewAsync(string userId, int bookId);
}
