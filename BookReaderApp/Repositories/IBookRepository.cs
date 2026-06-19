using BookReaderApp.Models;

namespace BookReaderApp.Repositories;

// Specialized repository for the book catalog. Extends the generic CRUD with a
// full-text-ish search across title, author, ISBN, description and genre.
public interface IBookRepository : IRepository<Book>
{
    // Paginated catalog query. When query is null/blank all books are returned;
    // otherwise matches the term against title, author name, ISBN, description and genre.
    Task<PagedResult<Book>> SearchPagedAsync(
        string? query, int page, int pageSize = PagedResult<Book>.DefaultPageSize);

    // Books "similar" to a source book: same author OR sharing any of the given genres,
    // excluding the given book ids. Ordered by title, capped at take. Builds recommendation rows.
    Task<IReadOnlyList<Book>> GetSimilarAsync(
        int authorId, IReadOnlyCollection<int> genreIds, IReadOnlyCollection<int> excludeBookIds, int take);

    // Looks up a catalog book by ISBN (trimmed, case-insensitive) so callers can reject
    // adding the same book twice. Returns null when no book has that ISBN.
    Task<Book?> GetByIsbnAsync(string isbn);
}
