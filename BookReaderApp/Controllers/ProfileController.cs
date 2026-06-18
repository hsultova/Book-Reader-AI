using BookReaderApp.Models;
using BookReaderApp.Models.ViewModels;
using BookReaderApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookReaderApp.Controllers;

[Authorize]
public class ProfileController : Controller
{
    // Avatars are capped at 2 MB and limited to common image types.
    private const long MaxAvatarBytes = 2 * 1024 * 1024;
    private static readonly string[] AllowedAvatarExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    private readonly IProfileService _profileService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public ProfileController(
        IProfileService profileService,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env)
    {
        _profileService = profileService;
        _userManager = userManager;
        _env = env;
    }

    // Public profile, viewable by any signed-in user. Defaults to the current user.
    [HttpGet]
    public async Task<IActionResult> Index(string? id)
    {
        var currentUserId = _userManager.GetUserId(User)!;
        var targetUserId = string.IsNullOrEmpty(id) ? currentUserId : id;

        var profile = await _profileService.GetProfileAsync(targetUserId, currentUserId);
        if (profile is null)
        {
            return NotFound();
        }

        return View(profile);
    }

    // Sets or clears the annual reading-challenge goal from the profile page modal.
    [HttpPost]
    public async Task<IActionResult> UpdateGoal(int? readingGoal)
    {
        if (readingGoal is < 0 or > 1000)
        {
            TempData["GoalError"] = "Reading goal must be between 0 and 1000.";
            return RedirectToAction(nameof(Index));
        }

        var userId = _userManager.GetUserId(User)!;
        var updated = await _profileService.UpdateReadingGoalAsync(userId, readingGoal);
        if (!updated)
        {
            TempData["GoalError"] = "Could not update your reading goal. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Settings()
    {
        var userId = _userManager.GetUserId(User)!;
        var model = await _profileService.GetSettingsAsync(userId);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Settings(ProfileSettingsViewModel model)
    {
        var userId = _userManager.GetUserId(User)!;

        string? newPicturePath = null;
        if (model.ProfilePicture is { Length: > 0 } file)
        {
            if (!IsValidAvatar(file, out var error))
            {
                ModelState.AddModelError(nameof(model.ProfilePicture), error);
            }
            else
            {
                newPicturePath = await SaveAvatarAsync(file, userId);
            }
        }

        if (!ModelState.IsValid)
        {
            // Preserve the current avatar in the redisplayed form.
            model.ExistingProfilePicturePath ??= (await _profileService.GetSettingsAsync(userId))?.ExistingProfilePicturePath;
            return View(model);
        }

        var updated = await _profileService.UpdateProfileAsync(userId, model, newPicturePath);
        if (!updated)
        {
            ModelState.AddModelError(string.Empty, "Could not save your profile. Please try again.");
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    private static bool IsValidAvatar(IFormFile file, out string error)
    {
        if (file.Length > MaxAvatarBytes)
        {
            error = "The image must be 2 MB or smaller.";
            return false;
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || !AllowedAvatarExtensions.Contains(ext))
        {
            error = "Please upload an image file (jpg, png, gif, or webp).";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private async Task<string> SaveAvatarAsync(IFormFile file, string userId)
    {
        var avatarsDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(avatarsDir);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{userId}{ext}";
        var fullPath = Path.Combine(avatarsDir, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Cache-bust so the new image shows immediately after re-upload.
        return $"/uploads/avatars/{fileName}?v={DateTime.UtcNow.Ticks}";
    }
}
