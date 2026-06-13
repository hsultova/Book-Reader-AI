using BookReaderApp.Models;
using BookReaderApp.Models.ViewModels;
using BookReaderApp.Repositories;

namespace BookReaderApp.Services;

// Orchestrates book CRUD over the generic repository and maps between the form view
// model and the Book entity. No ASP.NET types here — outcomes are returned as results.
public class BookService : IBookService
{
    private readonly IRepository<Book> _books;
    private readonly IAuthorRepository _authors;
    private readonly ILogger<BookService> _logger;

    public BookService(IRepository<Book> books, IAuthorRepository authors, ILogger<BookService> logger)
    {
        _books = books;
        _authors = authors;
        _logger = logger;
    }

    public Task<PagedResult<Book>> GetBooksAsync(int page, int pageSize = PagedResult<Book>.DefaultPageSize) =>
        _books.GetPagedAsync(page, pageSize);

    public Task<Book?> GetBookByIdAsync(int id) =>
        _books.GetByIdAsync(id);

    public async Task<BookSaveResult> CreateBookAsync(BookFormViewModel model)
    {
        var authorId = await ResolveAuthorAsync(model);
        var book = new Book();
        Apply(model, book, authorId);

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

        var authorId = await ResolveAuthorAsync(model);
        Apply(model, book, authorId);
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

    private async Task<int> ResolveAuthorAsync(BookFormViewModel model)
    {
        if (int.TryParse(model.AuthorValue, out var existingId) && existingId > 0)
            return existingId;

        var author = new Author { Name = model.AuthorValue!.Trim() };
        await _authors.AddAsync(author);
        await _authors.SaveChangesAsync();
        return author.Id;
    }

    private static void Apply(BookFormViewModel model, Book book, int authorId)
    {
        book.Title = model.Title;
        book.AuthorId = authorId;
        book.Isbn = model.Isbn;
        book.CoverImageUrl = model.CoverImageUrl;
        book.Description = model.Description;
        book.Genre = model.Genre;
    }
}
