using BookReaderApp.Data;
using BookReaderApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Repositories;

public class BookRepository : EfRepository<Book>, IBookRepository
{
    public BookRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<Book?> GetByIdAsync(int id) =>
        await Set
            .Include(b => b.Author)
            .Include(b => b.Genres)
            .FirstOrDefaultAsync(b => b.Id == id);

    public override Task<PagedResult<Book>> GetPagedAsync(
        int page, int pageSize = PagedResult<Book>.DefaultPageSize) =>
        SearchPagedAsync(null, page, pageSize);

    public async Task<PagedResult<Book>> SearchPagedAsync(
        string? query, int page, int pageSize = PagedResult<Book>.DefaultPageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = PagedResult<Book>.DefaultPageSize;

        var books = Set
            .Include(b => b.Author)
            .Include(b => b.Genres)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            // LIKE %term% is case-insensitive for ASCII in SQLite by default. Covers
            // title, author, ISBN and keywords in the description/genres in one pass.
            var pattern = $"%{query.Trim()}%";
            books = books.Where(b =>
                EF.Functions.Like(b.Title, pattern) ||
                (b.Author != null && EF.Functions.Like(b.Author.Name, pattern)) ||
                EF.Functions.Like(b.Isbn, pattern) ||
                (b.Description != null && EF.Functions.Like(b.Description, pattern)) ||
                b.Genres.Any(g => EF.Functions.Like(g.Name, pattern)));
        }

        var totalCount = await books.CountAsync();
        var items = await books
            .OrderBy(b => b.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Book>(items, page, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<Book>> GetSimilarAsync(
        int authorId, IReadOnlyCollection<int> genreIds, IReadOnlyCollection<int> excludeBookIds, int take) =>
        await Set
            .Include(b => b.Author)
            .Include(b => b.Genres)
            .Where(b => !excludeBookIds.Contains(b.Id)
                && (b.AuthorId == authorId || b.Genres.Any(g => genreIds.Contains(g.Id))))
            .OrderBy(b => b.Title)
            .Take(take)
            .ToListAsync();

    public async Task<Book?> GetByIsbnAsync(string isbn)
    {
        var normalized = isbn.Trim().ToLower();
        return await Set.FirstOrDefaultAsync(b => b.Isbn.ToLower() == normalized);
    }
}
