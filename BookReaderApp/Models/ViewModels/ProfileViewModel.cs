namespace BookReaderApp.Models.ViewModels;

// Read-only view of a user's public profile. IsOwnProfile drives whether the
// "Edit profile" link is shown to the viewer.
public class ProfileViewModel
{
    // Id of the profile's owner, needed when posting a friend request from this page.
    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public string? FavoriteGenre { get; set; }

    public int? ReadingGoal { get; set; }

    public string? ProfilePicturePath { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsOwnProfile { get; set; }

    // Relationship of this profile to the viewer; drives the friend button. Only
    // meaningful when IsOwnProfile is false.
    public FriendState FriendState { get; set; } = FriendState.None;

    // The pending request id when the viewer has received a request from this user
    // (so the profile can render Accept/Reject). Null otherwise.
    public int? PendingRequestId { get; set; }

    // Whether the viewer follows this user; drives the Follow/Unfollow button. Only
    // meaningful when IsOwnProfile is false.
    public bool IsFollowing { get; set; }

    // The annual reading challenge for the profile's owner (goal, progress, milestones).
    public ReadingChallengeViewModel Challenge { get; set; } = new();
}
