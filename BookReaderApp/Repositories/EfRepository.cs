using BookReaderApp.Data;
using BookReaderApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Repositories;

// EF Core implementation of the generic repository. Registered open-generically in DI
// (typeof(IRepository<>) -> typeof(EfRepository<>)) so any entity gets CRUD for free.
public class EfRepository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<T> Set;

    public EfRepository(ApplicationDbContext context)
    {
        Context = context;
        Set = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id) =>
        await Set.FindAsync(id);

    public virtual async Task<PagedResult<T>> GetPagedAsync(
        int page, int pageSize = PagedResult<T>.DefaultPageSize)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = PagedResult<T>.DefaultPageSize;
        }

        var totalCount = await Set.CountAsync();

        var items = await Set
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<T>(items, page, pageSize, totalCount);
    }

    public virtual async Task AddAsync(T entity) =>
        await Set.AddAsync(entity);

    public virtual void Update(T entity) =>
        Set.Update(entity);

    public virtual void Remove(T entity) =>
        Set.Remove(entity);

    public virtual Task<int> SaveChangesAsync() =>
        Context.SaveChangesAsync();
}
