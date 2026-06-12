using BookReaderApp.Models;
using BookReaderApp.Models.ViewModels;

namespace BookReaderApp.Services;

// Business logic for the book catalog. Controllers depend on this abstraction rather
// than the repository/DbContext directly, keeping them thin and the logic testable.
public interface IBookService
{
    Task<PagedResult<Book>> GetBooksAsync(int page, int pageSize = PagedResult<Book>.DefaultPageSize);

    Task<Book?> GetBookByIdAsync(int id);

    Task<BookSaveResult> CreateBookAsync(BookFormViewModel model);

    Task<BookSaveResult> UpdateBookAsync(int id, BookFormViewModel model);

    Task<bool> DeleteBookAsync(int id);
}
