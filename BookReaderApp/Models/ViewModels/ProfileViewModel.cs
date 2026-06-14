namespace BookReaderApp.Models.ViewModels;

// Read-only view of a user's public profile. IsOwnProfile drives whether the
// "Edit profile" link is shown to the viewer.
public class ProfileViewModel
{
    public string DisplayName { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public string? FavoriteGenre { get; set; }

    public int? ReadingGoal { get; set; }

    public string? ProfilePicturePath { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsOwnProfile { get; set; }
}
