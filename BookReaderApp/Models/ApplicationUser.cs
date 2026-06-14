using Microsoft.AspNetCore.Identity;

namespace BookReaderApp.Models;

// Extends the Identity user with reading-app profile fields. Keeping this here
// (rather than a separate domain model) lets Identity own the user lifecycle.
public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    // Short "About me" blurb shown on the profile page.
    public string? Bio { get; set; }

    public string? FavoriteGenre { get; set; }

    // Yearly books-to-read target.
    public int? ReadingGoal { get; set; }

    // Web-relative path to the uploaded avatar, e.g. /uploads/avatars/{id}.png.
    public string? ProfilePicturePath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // The user's personal bookshelf entries.
    public ICollection<UserBook> UserBooks { get; set; } = new List<UserBook>();

    // The user's custom shelves.
    public ICollection<Shelf> Shelves { get; set; } = new List<Shelf>();
}
