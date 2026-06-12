using BookReaderApp.Models;
using BookReaderApp.Models.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace BookReaderApp.Services;

// The "brain" for account flows: orchestrates Identity's UserManager/SignInManager,
// assigns default roles, and signs new users in. Controllers stay thin by delegating
// here. Identity's managers act as the data-access adapters for the user store, so no
// separate repository is introduced for this layer.
public class AccountService : IAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public async Task<RegistrationResult> RegisterAsync(RegisterViewModel model)
    {
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            return RegistrationResult.Failure(result.Errors.Select(e => e.Description));
        }

        await _userManager.AddToRoleAsync(user, AppRoles.User);
        await _signInManager.SignInAsync(user, isPersistent: false);
        _logger.LogInformation("New account registered for {Email}.", model.Email);

        return RegistrationResult.Success();
    }

    public async Task<LoginOutcome> LoginAsync(LoginViewModel model)
    {
        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} signed in.", model.Email);
            return LoginOutcome.Success;
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Account {Email} locked out.", model.Email);
            return LoginOutcome.LockedOut;
        }

        return LoginOutcome.Failed;
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User signed out.");
    }
}
