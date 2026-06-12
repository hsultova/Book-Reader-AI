namespace BookReaderApp.Services;

// Transport-agnostic outcomes returned by IAccountService. These keep ASP.NET
// concerns (IActionResult, ModelState) out of the business layer: the service
// decides *what* happened, the controller decides how to render it.

public sealed record RegistrationResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static RegistrationResult Success() => new(true, Array.Empty<string>());

    public static RegistrationResult Failure(IEnumerable<string> errors) =>
        new(false, errors.ToList());
}

public enum LoginOutcome
{
    Success,
    LockedOut,
    Failed
}
