using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Core.Options;

internal sealed class UAuthLoginOptionsValidator : IValidateOptions<UAuthLoginOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthLoginOptions options)
    {
        var errors = new List<string>();

        if (options.MaxFailedAttempts < 0)
            errors.Add("Login.MaxFailedAttempts cannot be negative.");

        if (options.MaxFailedAttempts > 100)
            errors.Add("Login.MaxFailedAttempts cannot exceed 100. Use 0 to disable lockout.");

        if (options.MaxFailedAttempts > 0 && options.LockoutMinutes <= 0)
            errors.Add("Login.LockoutMinutes must be greater than zero when lockout is enabled.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
