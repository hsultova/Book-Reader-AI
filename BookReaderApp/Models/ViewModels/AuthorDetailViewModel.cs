using BookReaderApp.Models;

namespace BookReaderApp.Models.ViewModels;

public class AuthorDetailViewModel
{
    public Author Author { get; set; } = null!;
    public int FollowerCount { get; set; }
    public bool IsFollowing { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool IsAdmin { get; set; }
}
