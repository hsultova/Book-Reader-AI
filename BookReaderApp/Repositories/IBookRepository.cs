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
}
