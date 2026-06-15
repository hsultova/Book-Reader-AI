using BookReaderApp.Models;

namespace BookReaderApp.Repositories;

// Specialized repository for review comments. Extends the generic CRUD with a batch
// lookup of comments grouped per review (with the commenting user loaded).
public interface IReviewCommentRepository : IRepository<ReviewComment>
{
    // Comments keyed by review id, each list oldest-first, with the commenting user loaded.
    // Reviews with no comments are omitted.
    Task<IReadOnlyDictionary<int, IReadOnlyList<ReviewComment>>> GetForReviewsAsync(
        IEnumerable<int> reviewIds);
}
