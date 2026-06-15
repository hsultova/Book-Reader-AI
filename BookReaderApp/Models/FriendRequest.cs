namespace BookReaderApp.Models;

// Lifecycle of a friend request. An Accepted row doubles as the friendship record,
// so there is no separate Friendship table (mirrors how ReviewLike is a single join row).
public enum FriendRequestStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2
}

// A directed friend request from Requester to Addressee. At most one row per
// ordered pair (enforced by a unique index on RequesterId + AddresseeId).
public class FriendRequest
{
    public int Id { get; set; }

    // The user who sent the request.
    public string RequesterId { get; set; } = string.Empty;

    public ApplicationUser? Requester { get; set; }

    // The user who received it and may accept or reject.
    public string AddresseeId { get; set; } = string.Empty;

    public ApplicationUser? Addressee { get; set; }

    public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Set when the addressee accepts or rejects.
    public DateTime? RespondedAt { get; set; }
}
