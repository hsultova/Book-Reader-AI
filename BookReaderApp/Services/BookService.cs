using BookReaderApp.Models;
using BookReaderApp.Models.ViewModels;
using BookReaderApp.Repositories;

namespace BookReaderApp.Services;

// Orchestrates book CRUD over the generic repository and maps between the form view
// model and the Book entity. No ASP.NET types here — outcomes are returned as results.
public class BookService : IBookService
{
    private readonly IBookRepository _books;
    private readonly IAuthorRepository _authors;
    private readonly IGenreRepository _genres;
    private readonly ILogger<BookService> _logger;

    public BookService(
        IBookRepository books,
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

    public Task<PagedResult<Book>> SearchBooksAsync(
        string? query, int page, int pageSize = PagedResult<Book>.DefaultPageSize) =>
        _books.SearchPagedAsync(query, page, pageSize);

    public Task<Book?> GetBookByIdAsync(int id) =>
        _books.GetByIdAsync(id);

    public async Task<BookSaveResult> CreateBookAsync(BookFormViewModel model)
    {
        // Reject duplicates up front: the same book (identified by ISBN) must not be added twice.
        var isbn = model.Isbn.Trim();
        if (await _books.GetByIsbnAsync(isbn) is not null)
        {
            return BookSaveResult.Duplicate(isbn);
        }

        var authorId = await ResolveAuthorAsync(model);
        var genreId = await ResolveGenreAsync(model);
        var book = new Book();
        Apply(model, book, authorId, genreId);

        await _books.AddAsync(book);
        await _books.SaveChangesAsync();

        _logger.LogInformation("Book {BookId} '{Title}' created.", book.Id, book.Title);
        return BookSaveResult.Success(book.Id);
    }

    public async Task<BulkBookSaveResult> CreateBooksAsync(IEnumerable<BookFormViewModel> models)
    {
        var created = 0;
        var skipped = new List<string>();

        // Cache name -> id within the batch so a repeated author/genre name doesn't
        // create duplicate rows (the per-name resolvers below always insert otherwise).
        var authorCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var genreCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Track ISBNs already added in this batch so a value repeated across the selection
        // isn't inserted twice (the DB check below only sees committed rows).
        var seenIsbns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var model in models)
        {
            if (string.IsNullOrWhiteSpace(model.Isbn))
            {
                skipped.Add(string.IsNullOrWhiteSpace(model.Title) ? "(untitled)" : model.Title);
                continue;
            }

            // Skip books already in the catalog, or duplicated within this same batch.
            var isbn = model.Isbn.Trim();
            if (!seenIsbns.Add(isbn) || await _books.GetByIsbnAsync(isbn) is not null)
            {
                skipped.Add(string.IsNullOrWhiteSpace(model.Title) ? "(untitled)" : model.Title);
                continue;
            }

            var authorId = await ResolveAuthorAsync(model, authorCache);
            var genreId = await ResolveGenreAsync(model, genreCache);
            var book = new Book();
            Apply(model, book, authorId, genreId);

            await _books.AddAsync(book);
            await _books.SaveChangesAsync();
            created++;
        }

        _logger.LogInformation("Bulk create: {Created} book(s) created, {Skipped} skipped.", created, skipped.Count);
        return new BulkBookSaveResult(created, skipped);
    }

    public async Task<BookSaveResult> UpdateBookAsync(int id, BookFormViewModel model)
    {
        var book = await _books.GetByIdAsync(id);
        if (book is null)
        {
            return BookSaveResult.NotFound();
        }

        // Don't let an edit collide with a different book's ISBN.
        var isbn = model.Isbn.Trim();
        var existing = await _books.GetByIsbnAsync(isbn);
        if (existing is not null && existing.Id != id)
        {
            return BookSaveResult.Duplicate(isbn);
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

        // A free-typed name reuses a matching author if one exists, so the same author
        // isn't stored twice; only a genuinely new name creates a row.
        var name = model.AuthorValue!.Trim();
        var existing = await _authors.GetByNameAsync(name);
        if (existing is not null)
            return existing.Id;

        var author = new Author { Name = name };
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

        // Reuse a matching genre if one exists rather than inserting a duplicate.
        var name = model.GenreValue.Trim();
        var existing = await _genres.GetByNameAsync(name);
        if (existing is not null)
            return existing.Id;

        var genre = new Genre { Name = name };
        await _genres.AddAsync(genre);
        await _genres.SaveChangesAsync();
        return genre.Id;
    }

    // Cache-aware variant for bulk create: reuses existing authors and dedupes new ones
    // by name within the batch so a repeated name never produces duplicate rows.
    private async Task<int> ResolveAuthorAsync(BookFormViewModel model, Dictionary<string, int> cache)
    {
        if (int.TryParse(model.AuthorValue, out var existingId) && existingId > 0)
            return existingId;

        var name = model.AuthorValue!.Trim();
        if (cache.TryGetValue(name, out var cachedId))
            return cachedId;

        var existing = await _authors.GetByNameAsync(name);
        if (existing is not null)
        {
            cache[name] = existing.Id;
            return existing.Id;
        }

        var author = new Author { Name = name };
        await _authors.AddAsync(author);
        await _authors.SaveChangesAsync();
        cache[name] = author.Id;
        return author.Id;
    }

    // Cache-aware variant for bulk create: reuses existing genres and dedupes new ones
    // by name within the batch.
    private async Task<int?> ResolveGenreAsync(BookFormViewModel model, Dictionary<string, int> cache)
    {
        if (string.IsNullOrWhiteSpace(model.GenreValue))
            return null;

        if (int.TryParse(model.GenreValue, out var existingId) && existingId > 0)
            return existingId;

        var name = model.GenreValue.Trim();
        if (cache.TryGetValue(name, out var cachedId))
            return cachedId;

        var existing = await _genres.GetByNameAsync(name);
        if (existing is not null)
        {
            cache[name] = existing.Id;
            return existing.Id;
        }

        var genre = new Genre { Name = name };
        await _genres.AddAsync(genre);
        await _genres.SaveChangesAsync();
        cache[name] = genre.Id;
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
