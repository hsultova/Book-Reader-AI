using BookReaderApp.Data;
using BookReaderApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Repositories;

public class BookRepository : EfRepository<Book>
{
    public BookRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<Book?> GetByIdAsync(int id) =>
        await Set
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .FirstOrDefaultAsync(b => b.Id == id);

    public override async Task<PagedResult<Book>> GetPagedAsync(
        int page, int pageSize = PagedResult<Book>.DefaultPageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = PagedResult<Book>.DefaultPageSize;

        var totalCount = await Set.CountAsync();
        var items = await Set
            .Include(b => b.Author)
            .Include(b => b.Genre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Book>(items, page, pageSize, totalCount);
    }
}
