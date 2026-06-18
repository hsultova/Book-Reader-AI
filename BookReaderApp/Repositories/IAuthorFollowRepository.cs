using BookReaderApp.Models;

namespace BookReaderApp.Repositories;

public interface IAuthorFollowRepository : IRepository<AuthorFollow>
{
    Task<AuthorFollow?> GetAsync(string userId, int authorId);
    Task<bool> IsFollowingAsync(string userId, int authorId);
    Task<int> GetFollowerCountAsync(int authorId);
}
