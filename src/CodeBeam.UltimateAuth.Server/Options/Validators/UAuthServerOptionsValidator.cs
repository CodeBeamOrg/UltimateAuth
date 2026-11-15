
using CodeBeam.UltimateAuth.Core;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthServerOptionsValidator : IValidateOptions<UAuthServerOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthServerOptions options)
    {
        var rateError = RateLimiting.UAuthRateLimitValidation.Validate(options.RateLimiting);
        if (rateError is not null)
            return ValidateOptionsResult.Fail(rateError);

        if (string.IsNullOrWhiteSpace(options.Endpoints.BasePath))
            return ValidateOptionsResult.Fail("BasePath must be specified.");

        if (options.Endpoints.BasePath.Contains("//"))
            return ValidateOptionsResult.Fail("BasePath cannot contain '//'.");

        var allowedModes = options.AllowedModes;

        if (allowedModes is { Count: > 0 })
        {
            foreach (var mode in allowedModes)
            {
                if (!Enum.IsDefined(typeof(UAuthMode), mode))
                    return ValidateOptionsResult.Fail($"Invalid UAuthMode value: {mode}");

                // TODO: Keep unsupported modes disabled until their implementation and conformance tests are complete.
                if (mode is UAuthMode.SemiHybrid)
                {
                    return ValidateOptionsResult.Fail($"Auth mode '{mode}' is not implemented yet and cannot be enabled.");
                }
            }
        }

        var anySessionModeAllowed =
            allowedModes is null ||
            allowedModes.Count == 0 ||
            allowedModes.Contains(UAuthMode.PureOpaque) ||
            allowedModes.Contains(UAuthMode.Hybrid) ||
            allowedModes.Contains(UAuthMode.SemiHybrid);

        if (anySessionModeAllowed)
        {
            if (options.Session.Lifetime <= TimeSpan.Zero)
            {
                return ValidateOptionsResult.Fail("Session.Lifetime must be greater than zero.");
            }

            if (options.Session.MaxLifetime is { } maxLifetime)
            {
                if (maxLifetime <= TimeSpan.Zero)
                {
                    return ValidateOptionsResult.Fail("Session.MaxLifetime must be greater than zero when specified.");
                }

                if (options.Token.AccessTokenLifetime > maxLifetime)
                {
                    return ValidateOptionsResult.Fail("Token.AccessTokenLifetime cannot exceed Session.MaxLifetime.");
                }
            }
        }

        return ValidateOptionsResult.Success;
    }
}
