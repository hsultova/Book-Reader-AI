using BookReaderApp.Models.ViewModels;

namespace BookReaderApp.Services;

// Business logic for account registration and authentication. Controllers depend
// on this abstraction (not UserManager/SignInManager directly) so the orchestration
// is testable and reusable across HTTP and any future API surface.
public interface IAccountService
{
    Task<RegistrationResult> RegisterAsync(RegisterViewModel model);

    Task<LoginOutcome> LoginAsync(LoginViewModel model);

    Task LogoutAsync();
}
