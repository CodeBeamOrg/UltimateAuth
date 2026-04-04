using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

internal sealed class UAuthServerSessionOptionsValidator : IValidateOptions<UAuthServerOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthServerOptions options)
    {
        var errors = new List<string>();
        var session = options.Session;

        if (session.Lifetime <= TimeSpan.Zero)
            errors.Add("Session.Lifetime must be greater than zero.");

        if (session.MaxLifetime.HasValue && session.MaxLifetime <= TimeSpan.Zero)
            errors.Add("Session.MaxLifetime must be greater than zero when specified.");

        if (session.MaxLifetime.HasValue &&
            session.MaxLifetime < session.Lifetime)
            errors.Add("Session.MaxLifetime must be greater than or equal to Session.Lifetime.");

        if (session.IdleTimeout.HasValue && session.IdleTimeout < TimeSpan.Zero)
            errors.Add("Session.IdleTimeout cannot be negative.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
