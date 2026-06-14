using System.ComponentModel.DataAnnotations;

namespace BookReaderApp.Models.ViewModels;

// Backs the account settings edit form. The avatar is an upload; the existing
// path is carried along so the current picture can be re-shown after a failed post.
public class ProfileSettingsViewModel
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "Display name")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "About me")]
    public string? Bio { get; set; }

    [StringLength(100)]
    [Display(Name = "Favorite genre")]
    public string? FavoriteGenre { get; set; }

    [Range(0, 1000)]
    [Display(Name = "Reading goal (books per year)")]
    public int? ReadingGoal { get; set; }

    [Display(Name = "Profile picture")]
    public IFormFile? ProfilePicture { get; set; }

    public string? ExistingProfilePicturePath { get; set; }
}
