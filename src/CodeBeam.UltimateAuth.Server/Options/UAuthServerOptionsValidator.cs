using CodeBeam.UltimateAuth.Core;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthServerOptionsValidator : IValidateOptions<UAuthServerOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthServerOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.RoutePrefix))
        {
            return ValidateOptionsResult.Fail( "RoutePrefix must be specified.");
        }

        if (options.RoutePrefix.Contains("//"))
        {
            return ValidateOptionsResult.Fail("RoutePrefix cannot contain '//'.");
        }

        if (options.Mode.HasValue && !Enum.IsDefined(typeof(UAuthMode), options.Mode))
        {
            return ValidateOptionsResult.Fail($"Invalid UAuthMode: {options.Mode}");
        }

        if (options.Mode != UAuthMode.PureJwt)
        {
            if (options.Session.Lifetime <= TimeSpan.Zero)
            {
                return ValidateOptionsResult.Fail("Session.Lifetime must be greater than zero.");
            }

            if (options.Session.MaxLifetime is not null &&
                options.Session.MaxLifetime <= TimeSpan.Zero)
            {
                return ValidateOptionsResult.Fail(
                    "Session.MaxLifetime must be greater than zero when specified.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
