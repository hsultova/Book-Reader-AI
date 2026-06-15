namespace BookReaderApp.Messaging.Dtos;

// A message as exposed to callers. Carries only ids/strings — the host maps SenderId to a
// display name and decides "is this mine?" against the current user.
public record MessageDto(
    int Id,
    int ConversationId,
    string SenderId,
    string Content,
    DateTime CreatedAt,
    bool IsRead);
