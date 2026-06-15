using BookReaderApp.Models;
using BookReaderApp.Models.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace BookReaderApp.Services;

// Orchestrates Identity's UserManager to read and persist profile fields. Mirrors
// AccountService: the manager is the data-access adapter, so no separate repository
// is introduced. Avatar files are saved by the controller; only the path arrives here.
public class ProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFriendRequestService _friendRequests;
    private readonly IFollowService _follows;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        UserManager<ApplicationUser> userManager,
        IFriendRequestService friendRequests,
        IFollowService follows,
        ILogger<ProfileService> logger)
    {
        _userManager = userManager;
        _friendRequests = friendRequests;
        _follows = follows;
        _logger = logger;
    }

    public async Task<ProfileViewModel?> GetProfileAsync(string targetUserId, string currentUserId)
    {
        var user = await _userManager.FindByIdAsync(targetUserId);
        if (user is null)
        {
            return null;
        }

        var isOwnProfile = targetUserId == currentUserId;

        // Resolve the friend and follow relationships only when viewing someone else.
        var friendState = FriendState.None;
        int? pendingRequestId = null;
        var isFollowing = false;
        if (!isOwnProfile)
        {
            friendState = await _friendRequests.GetRelationshipAsync(currentUserId, targetUserId);
            if (friendState == FriendState.IncomingPending)
            {
                pendingRequestId = await _friendRequests.GetIncomingRequestIdAsync(currentUserId, targetUserId);
            }

            isFollowing = await _follows.IsFollowingAsync(currentUserId, targetUserId);
        }

        return new ProfileViewModel
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            FavoriteGenre = user.FavoriteGenre,
            ReadingGoal = user.ReadingGoal,
            ProfilePicturePath = user.ProfilePicturePath,
            CreatedAt = user.CreatedAt,
            IsOwnProfile = isOwnProfile,
            FriendState = friendState,
            PendingRequestId = pendingRequestId,
            IsFollowing = isFollowing
        };
    }

    public async Task<ProfileSettingsViewModel?> GetSettingsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        return new ProfileSettingsViewModel
        {
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            FavoriteGenre = user.FavoriteGenre,
            ReadingGoal = user.ReadingGoal,
            ExistingProfilePicturePath = user.ProfilePicturePath
        };
    }

    public async Task<bool> UpdateProfileAsync(string userId, ProfileSettingsViewModel model, string? newPicturePath)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }

        user.DisplayName = model.DisplayName;
        user.Bio = model.Bio;
        user.FavoriteGenre = model.FavoriteGenre;
        user.ReadingGoal = model.ReadingGoal;

        // Only overwrite the avatar when a new one was uploaded.
        if (newPicturePath is not null)
        {
            user.ProfilePicturePath = newPicturePath;
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Profile update failed for user {UserId}: {Errors}",
                userId, string.Join("; ", result.Errors.Select(e => e.Description)));
            return false;
        }

        _logger.LogInformation("Profile updated for user {UserId}.", userId);
        return true;
    }
}
