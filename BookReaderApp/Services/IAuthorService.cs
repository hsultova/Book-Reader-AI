using BookReaderApp.Models;

namespace BookReaderApp.Services;

public interface IAuthorService
{
    Task<IReadOnlyList<Author>> GetAllAuthorsAsync();
    Task<Author?> GetAuthorByIdAsync(int id);

    // Returns the author with all their books loaded (for the detail page).
    Task<Author?> GetAuthorWithBooksAsync(int id);

    // Updates the author's editable fields. Returns false if no author with that id exists.
    Task<bool> UpdateAuthorAsync(int id, string name, string? description, string? photo);

    Task<int> GetFollowerCountAsync(int authorId);
    Task<bool> IsFollowingAsync(string userId, int authorId);
    Task FollowAsync(string userId, int authorId);
    Task UnfollowAsync(string userId, int authorId);
}
