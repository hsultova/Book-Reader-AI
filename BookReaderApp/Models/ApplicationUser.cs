using Microsoft.AspNetCore.Identity;

namespace BookReaderApp.Models;

// Extends the Identity user with reading-app profile fields. Keeping this here
// (rather than a separate domain model) lets Identity own the user lifecycle.
public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // The user's personal bookshelf entries.
    public ICollection<UserBook> UserBooks { get; set; } = new List<UserBook>();
}
