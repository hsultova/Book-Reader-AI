namespace BookReaderApp.Models;

// A lightweight projection of a user for rendering an avatar stack (e.g. the readers of a
// book on the Details page). Carries just enough to link to the profile and show a face.
public record ReaderAvatar(string UserId, string DisplayName, string? ProfilePicturePath);
