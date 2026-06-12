using BookReaderApp.Models;
using BookReaderApp.Models.ViewModels;
using BookReaderApp.Repositories;

namespace BookReaderApp.Services;

// Orchestrates book CRUD over the generic repository and maps between the form view
// model and the Book entity. No ASP.NET types here — outcomes are returned as results.
public class BookService : IBookService
{
    private readonly IRepository<Book> _books;
    private readonly ILogger<BookService> _logger;

    public BookService(IRepository<Book> books, ILogger<BookService> logger)
    {
        _books = books;
        _logger = logger;
    }

    public Task<PagedResult<Book>> GetBooksAsync(int page, int pageSize = PagedResult<Book>.DefaultPageSize) =>
        _books.GetPagedAsync(page, pageSize);

    public Task<Book?> GetBookByIdAsync(int id) =>
        _books.GetByIdAsync(id);

    public async Task<BookSaveResult> CreateBookAsync(BookFormViewModel model)
    {
        var book = new Book();
        Apply(model, book);

        await _books.AddAsync(book);
        await _books.SaveChangesAsync();

        _logger.LogInformation("Book {BookId} '{Title}' created.", book.Id, book.Title);
        return BookSaveResult.Success(book.Id);
    }

    public async Task<BookSaveResult> UpdateBookAsync(int id, BookFormViewModel model)
    {
        var book = await _books.GetByIdAsync(id);
        if (book is null)
        {
            return BookSaveResult.NotFound();
        }

        Apply(model, book);
        _books.Update(book);
        await _books.SaveChangesAsync();

        _logger.LogInformation("Book {BookId} updated.", book.Id);
        return BookSaveResult.Success(book.Id);
    }

    public async Task<bool> DeleteBookAsync(int id)
    {
        var book = await _books.GetByIdAsync(id);
        if (book is null)
        {
            return false;
        }

        _books.Remove(book);
        await _books.SaveChangesAsync();

        _logger.LogInformation("Book {BookId} deleted.", id);
        return true;
    }

    private static void Apply(BookFormViewModel model, Book book)
    {
        book.Title = model.Title;
        book.Author = model.Author;
        book.Isbn = model.Isbn;
        book.CoverImageUrl = model.CoverImageUrl;
        book.Description = model.Description;
        book.Genre = model.Genre;
    }
}
