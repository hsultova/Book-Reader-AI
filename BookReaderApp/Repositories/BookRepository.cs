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
            .Include(b => b.Genre)
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
            .Include(b => b.Genre)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            // LIKE %term% is case-insensitive for ASCII in SQLite by default. Covers
            // title, author, ISBN and keywords in the description/genre in one pass.
            var pattern = $"%{query.Trim()}%";
            books = books.Where(b =>
                EF.Functions.Like(b.Title, pattern) ||
                (b.Author != null && EF.Functions.Like(b.Author.Name, pattern)) ||
                EF.Functions.Like(b.Isbn, pattern) ||
                (b.Description != null && EF.Functions.Like(b.Description, pattern)) ||
                (b.Genre != null && EF.Functions.Like(b.Genre.Name, pattern)));
        }

        var totalCount = await books.CountAsync();
        var items = await books
            .OrderBy(b => b.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Book>(items, page, pageSize, totalCount);
    }
}
