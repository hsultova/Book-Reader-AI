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
    private readonly IGenreRepository _genres;
    private readonly ILogger<BookService> _logger;

    public BookService(
        IRepository<Book> books,
        IAuthorRepository authors,
        IGenreRepository genres,
        ILogger<BookService> logger)
    {
        _books = books;
        _authors = authors;
        _genres = genres;
        _logger = logger;
    }

    public Task<PagedResult<Book>> GetBooksAsync(int page, int pageSize = PagedResult<Book>.DefaultPageSize) =>
        _books.GetPagedAsync(page, pageSize);

    public Task<Book?> GetBookByIdAsync(int id) =>
        _books.GetByIdAsync(id);

    public async Task<BookSaveResult> CreateBookAsync(BookFormViewModel model)
    {
        var authorId = await ResolveAuthorAsync(model);
        var genreId = await ResolveGenreAsync(model);
        var book = new Book();
        Apply(model, book, authorId, genreId);

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
        var genreId = await ResolveGenreAsync(model);
        Apply(model, book, authorId, genreId);
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

    private async Task<int?> ResolveGenreAsync(BookFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.GenreValue))
            return null;

        if (int.TryParse(model.GenreValue, out var existingId) && existingId > 0)
            return existingId;

        var genre = new Genre { Name = model.GenreValue.Trim() };
        await _genres.AddAsync(genre);
        await _genres.SaveChangesAsync();
        return genre.Id;
    }

    private static void Apply(BookFormViewModel model, Book book, int authorId, int? genreId)
    {
        book.Title = model.Title;
        book.AuthorId = authorId;
        book.Isbn = model.Isbn;
        book.CoverImageUrl = model.CoverImageUrl;
        book.Description = model.Description;
        book.GenreId = genreId;
    }
}
