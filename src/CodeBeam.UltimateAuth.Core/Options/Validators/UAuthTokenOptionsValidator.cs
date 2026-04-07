using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Core.Options;

internal sealed class UAuthTokenOptionsValidator : IValidateOptions<UAuthTokenOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthTokenOptions options)
    {
        var errors = new List<string>();

        if (!options.IssueJwt && !options.IssueOpaque)
            errors.Add("Token: At least one of IssueJwt or IssueOpaque must be enabled.");

        if (options.AccessTokenLifetime <= TimeSpan.Zero)
            errors.Add("Token.AccessTokenLifetime must be greater than zero.");

        if (options.IssueRefresh)
        {
            if (options.RefreshTokenLifetime <= TimeSpan.Zero)
                errors.Add("Token.RefreshTokenLifetime must be greater than zero when IssueRefresh is enabled.");

            if (options.RefreshTokenLifetime <= options.AccessTokenLifetime)
                errors.Add("Token.RefreshTokenLifetime must be greater than Token.AccessTokenLifetime.");
        }

        if (options.IssueJwt)
        {
            if (string.IsNullOrWhiteSpace(options.Issuer) || options.Issuer.Trim().Length < 3)
                errors.Add("Token.Issuer must be at least 3 characters when IssueJwt is enabled.");

            if (string.IsNullOrWhiteSpace(options.Audience) || options.Audience.Trim().Length < 3)
                errors.Add("Token.Audience must be at least 3 characters when IssueJwt is enabled.");
        }

        if (options.IssueOpaque)
        {
            if (options.OpaqueIdBytes < 16)
                errors.Add("Token.OpaqueIdBytes must be at least 16 bytes (128-bit entropy).");

            if (options.OpaqueIdBytes > 128)
                errors.Add("Token.OpaqueIdBytes must not exceed 128 bytes.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
