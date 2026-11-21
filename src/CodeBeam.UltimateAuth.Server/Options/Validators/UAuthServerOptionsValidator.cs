
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

        if (options.AuthMode is { } configuredMode)
        {
            if (!Enum.IsDefined(configuredMode))
                return ValidateOptionsResult.Fail($"Invalid AuthMode value: {configuredMode}.");

            if (options.ApplicationProfileModes.Count > 0)
                return ValidateOptionsResult.Fail("AuthMode and ApplicationProfileModes cannot be configured together.");
        }

        foreach (var (profile, mode) in options.ApplicationProfileModes)
        {
            if (!Enum.IsDefined(profile))
                return ValidateOptionsResult.Fail($"Invalid application profile: {profile}.");

            if (!Enum.IsDefined(mode))
                return ValidateOptionsResult.Fail($"Invalid authentication mode: {mode}.");
        }

        var configuredModes = options.AuthMode is { } selected
            ? new[] { selected }
            : options.ApplicationProfileModes.Values.Distinct();

        foreach (var mode in configuredModes)
        {
            if (options.AllowedModes is { Count: > 0 } allowed && !allowed.Contains(mode))
            {
                return ValidateOptionsResult.Fail($"Configured authentication mode '{mode}' is not allowed.");
            }
        }

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
