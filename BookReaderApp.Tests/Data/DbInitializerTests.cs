using BookReaderApp.Data;
using BookReaderApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookReaderApp.Tests.Data;

public class DbInitializerTests
{
    private const string AdminPassword = "Admin#12345";
    private const string UserPassword = "User#12345";

    // Builds a real Identity stack (UserManager/RoleManager) backed by an
    // isolated in-memory database so the seeding logic runs against actual
    // Identity validators and password hashers, not mocks.
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"seed-tests-{Guid.NewGuid()}"));

        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        return services.BuildServiceProvider();
    }

    private static async Task SeedAsync(IServiceProvider sp)
    {
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        await DbInitializer.SeedAsync(userManager, roleManager, AdminPassword, UserPassword);
    }

    [Fact]
    public async Task SeedAsync_OnEmptyDatabase_CreatesAllRoles()
    {
        using var sp = BuildProvider();
        await SeedAsync(sp);

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in AppRoles.All)
        {
            Assert.True(await roleManager.RoleExistsAsync(role), $"Role '{role}' was not created.");
        }
    }

    [Fact]
    public async Task SeedAsync_OnEmptyDatabase_CreatesAdminUserInAdminRole()
    {
        using var sp = BuildProvider();
        await SeedAsync(sp);

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync(DbInitializer.AdminEmail);

        Assert.NotNull(admin);
        Assert.True(await userManager.IsInRoleAsync(admin!, AppRoles.Admin));
        Assert.True(await userManager.CheckPasswordAsync(admin!, AdminPassword));
    }

    [Fact]
    public async Task SeedAsync_OnEmptyDatabase_CreatesTestUserInUserRole()
    {
        using var sp = BuildProvider();
        await SeedAsync(sp);

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(DbInitializer.TestUserEmail);

        Assert.NotNull(user);
        Assert.True(await userManager.IsInRoleAsync(user!, AppRoles.User));
        Assert.True(await userManager.CheckPasswordAsync(user!, UserPassword));
    }

    [Fact]
    public async Task SeedAsync_StoresPasswordsHashed_NotPlaintext()
    {
        using var sp = BuildProvider();
        await SeedAsync(sp);

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync(DbInitializer.AdminEmail);

        Assert.NotNull(admin!.PasswordHash);
        Assert.NotEqual(AdminPassword, admin.PasswordHash);
    }

    [Fact]
    public async Task SeedAsync_RunTwice_IsIdempotent()
    {
        using var sp = BuildProvider();
        await SeedAsync(sp);
        await SeedAsync(sp);

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();

        Assert.Equal(2, userManager.Users.Count());
        Assert.Equal(AppRoles.All.Count, roleManager.Roles.Count());
    }
}
