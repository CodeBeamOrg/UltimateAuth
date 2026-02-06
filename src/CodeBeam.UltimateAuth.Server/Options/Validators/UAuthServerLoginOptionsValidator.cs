using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

internal sealed class UAuthServerLoginOptionsValidator : IValidateOptions<UAuthServerOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthServerOptions options)
    {
        var errors = new List<string>();
        var login = options.Login;

        if (login.MaxFailedAttempts < 0)
            errors.Add("Login.MaxFailedAttempts cannot be negative.");

        if (login.LockoutMinutes < 0)
            errors.Add("Login.LockoutMinutes cannot be negative.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
