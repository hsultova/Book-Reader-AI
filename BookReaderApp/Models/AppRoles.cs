namespace BookReaderApp.Models;

// Single source of truth for role names, reused by the seeder and
// [Authorize(Roles = ...)] attributes to avoid magic strings.
public static class AppRoles
{
    public const string User = "User";
    public const string Moderator = "Moderator";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = new[] { User, Moderator, Admin };
}
