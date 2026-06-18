using BookReaderApp.Models;
using BookReaderApp.Repositories;

namespace BookReaderApp.Services;

public class AuthorService : IAuthorService
{
    private readonly IAuthorRepository _authors;
    private readonly IAuthorFollowRepository _authorFollows;
    private readonly ILogger<AuthorService> _logger;

    public AuthorService(
        IAuthorRepository authors,
        IAuthorFollowRepository authorFollows,
        ILogger<AuthorService> logger)
    {
        _authors = authors;
        _authorFollows = authorFollows;
        _logger = logger;
    }

    public Task<IReadOnlyList<Author>> GetAllAuthorsAsync() =>
        _authors.GetAllAsync();

    public Task<Author?> GetAuthorByIdAsync(int id) =>
        _authors.GetByIdAsync(id);

    public Task<Author?> GetAuthorWithBooksAsync(int id) =>
        _authors.GetWithBooksAsync(id);

    public Task<int> GetFollowerCountAsync(int authorId) =>
        _authorFollows.GetFollowerCountAsync(authorId);

    public Task<bool> IsFollowingAsync(string userId, int authorId) =>
        _authorFollows.IsFollowingAsync(userId, authorId);

    public async Task FollowAsync(string userId, int authorId)
    {
        if (await _authorFollows.IsFollowingAsync(userId, authorId))
            return;

        await _authorFollows.AddAsync(new AuthorFollow
        {
            UserId = userId,
            AuthorId = authorId,
            CreatedAt = DateTime.UtcNow
        });
        await _authorFollows.SaveChangesAsync();
        _logger.LogInformation("User {UserId} followed author {AuthorId}.", userId, authorId);
    }

    public async Task UnfollowAsync(string userId, int authorId)
    {
        var follow = await _authorFollows.GetAsync(userId, authorId);
        if (follow is null)
            return;

        _authorFollows.Remove(follow);
        await _authorFollows.SaveChangesAsync();
        _logger.LogInformation("User {UserId} unfollowed author {AuthorId}.", userId, authorId);
    }
}
