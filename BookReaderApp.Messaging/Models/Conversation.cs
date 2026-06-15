namespace BookReaderApp.Messaging.Models;

// A 1:1 message thread between two users. The participant pair is normalized so
// User1Id is ordinally <= User2Id and carries a unique index on (User1Id, User2Id),
// guaranteeing exactly one conversation per pair regardless of who opened it first
// (mirrors the unique-index-as-business-rule approach used for FriendRequest/UserBook).
public class Conversation
{
    public int Id { get; set; }

    // The lexicographically smaller of the two participant user ids.
    public string User1Id { get; set; } = string.Empty;

    // The lexicographically larger of the two participant user ids.
    public string User2Id { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Bumped on every send so the conversation list can order by most-recent activity.
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public ICollection<DirectMessage> Messages { get; set; } = new List<DirectMessage>();

    // True when userId is one of the two participants.
    public bool HasParticipant(string userId) =>
        User1Id == userId || User2Id == userId;

    // The participant who isn't userId (empty if userId isn't a participant).
    public string OtherParticipant(string userId) =>
        User1Id == userId ? User2Id : User2Id == userId ? User1Id : string.Empty;

    // Orders a pair so the same two users always map to the same (User1Id, User2Id),
    // which the unique index relies on. Ordinal compare matches EF's string handling.
    public static (string User1Id, string User2Id) NormalizePair(string a, string b) =>
        string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);
}
