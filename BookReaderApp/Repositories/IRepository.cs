using BookReaderApp.Models;

namespace BookReaderApp.Repositories;

// Generic data-access abstraction. Services depend on this rather than the DbContext
// so business logic stays decoupled from EF Core and is testable. Concrete entities
// can extend this with specialized query methods (see IUserBookRepository).
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);

    Task<PagedResult<T>> GetPagedAsync(int page, int pageSize = PagedResult<T>.DefaultPageSize);

    Task AddAsync(T entity);

    void Update(T entity);

    void Remove(T entity);

    Task<int> SaveChangesAsync();
}
