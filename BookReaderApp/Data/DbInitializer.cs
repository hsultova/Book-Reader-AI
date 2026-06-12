using BookReaderApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookReaderApp.Data;

// Seeds the roles and the initial admin/test accounts. Split into a host-facing
// entry point (SeedAsync) and a manager-facing core (SeedAsync overload) so the
// seeding logic can be unit-tested against an in-memory store without a web host.
public static class DbInitializer
{
    public const string AdminEmail = "admin@bookreader.local";
    public const string TestUserEmail = "user@bookreader.local";

    // Dev-only fallbacks. In any real environment these come from configuration
    // (user-secrets / environment variables), never source control.
    private const string DefaultAdminPassword = "Admin#12345";
    private const string DefaultUserPassword = "User#12345";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var config = sp.GetRequiredService<IConfiguration>();

        var adminPassword = config["Seed:AdminPassword"] ?? DefaultAdminPassword;
        var userPassword = config["Seed:UserPassword"] ?? DefaultUserPassword;

        await SeedAsync(userManager, roleManager, adminPassword, userPassword);
        await SeedBooksAsync(db, userManager);
    }

    // Seeds a sample book and places it on the test user's shelf so "My Books" has
    // something to show out of the box. Idempotent: only acts when the catalog is empty.
    private static async Task SeedBooksAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        if (await db.Books.AnyAsync())
        {
            return;
        }

        var book = new Book
        {
            Title = "The Pragmatic Programmer",
            Author = "Andrew Hunt, David Thomas",
            Isbn = "978-0135957059",
            Genre = "Software",
            Description = "A classic guide to the craft of software development.",
            CoverImageUrl = "https://covers.openlibrary.org/b/isbn/9780135957059-L.jpg"
        };
        db.Books.Add(book);
        await db.SaveChangesAsync();

        var testUser = await userManager.FindByEmailAsync(TestUserEmail);
        if (testUser is not null &&
            !await db.UserBooks.AnyAsync(ub => ub.UserId == testUser.Id && ub.BookId == book.Id))
        {
            db.UserBooks.Add(new UserBook { UserId = testUser.Id, BookId = book.Id });
            await db.SaveChangesAsync();
        }
    }

    // Idempotent: safe to run on every startup. Returns nothing; throws if a
    // create operation fails so seeding problems surface loudly at boot.
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        string adminPassword,
        string userPassword)
    {
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await ThrowIfFailed(roleManager.CreateAsync(new IdentityRole(role)));
            }
        }

        await EnsureUserAsync(userManager, AdminEmail, "Administrator", adminPassword, AppRoles.Admin);
        await EnsureUserAsync(userManager, TestUserEmail, "Test User", userPassword, AppRoles.User);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string displayName,
        string password,
        string role)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName
        };

        await ThrowIfFailed(userManager.CreateAsync(user, password));
        await ThrowIfFailed(userManager.AddToRoleAsync(user, role));
    }

    private static async Task ThrowIfFailed(Task<IdentityResult> resultTask)
    {
        var result = await resultTask;
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException($"Identity seeding failed: {errors}");
        }
    }
}
