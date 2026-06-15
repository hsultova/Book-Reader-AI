namespace BookReaderApp.Models;

// A one-way follow from Follower to Followee. Following needs no approval and grants
// no messaging; it only means the follower's home feed includes the followee's public
// activity. At most one row per ordered pair (unique index on FollowerId + FolloweeId).
public class Follow
{
    public int Id { get; set; }

    // The user who follows.
    public string FollowerId { get; set; } = string.Empty;

    public ApplicationUser? Follower { get; set; }

    // The user being followed.
    public string FolloweeId { get; set; } = string.Empty;

    public ApplicationUser? Followee { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
