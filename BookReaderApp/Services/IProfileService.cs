using BookReaderApp.Models.ViewModels;

namespace BookReaderApp.Services;

// Business logic for viewing and editing user profiles. Controllers depend on this
// abstraction (not UserManager directly) so the orchestration stays testable. File
// persistence is the controller's job, so this layer never touches IFormFile.
public interface IProfileService
{
    Task<ProfileViewModel?> GetProfileAsync(string userId, bool isOwnProfile);

    Task<ProfileSettingsViewModel?> GetSettingsAsync(string userId);

    // newPicturePath is the already-saved avatar path, or null to keep the current one.
    Task<bool> UpdateProfileAsync(string userId, ProfileSettingsViewModel model, string? newPicturePath);
}
