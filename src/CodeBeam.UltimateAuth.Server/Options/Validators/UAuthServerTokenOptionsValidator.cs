using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

internal sealed class UAuthServerTokenOptionsValidator : IValidateOptions<UAuthServerOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthServerOptions options)
    {
        var errors = new List<string>();
        var tokens = options.Tokens;

        if (!tokens.IssueJwt && !tokens.IssueOpaque)
            errors.Add("Token: At least one of IssueJwt or IssueOpaque must be enabled.");

        if (tokens.AccessTokenLifetime <= TimeSpan.Zero)
            errors.Add("Token.AccessTokenLifetime must be greater than zero.");

        if (tokens.IssueRefresh)
        {
            if (tokens.RefreshTokenLifetime <= TimeSpan.Zero)
                errors.Add("Token.RefreshTokenLifetime must be greater than zero when IssueRefresh is enabled.");

            if (tokens.RefreshTokenLifetime <= tokens.AccessTokenLifetime)
                errors.Add("Token.RefreshTokenLifetime must be greater than Token.AccessTokenLifetime.");
        }

        if (tokens.IssueJwt)
        {
            if (string.IsNullOrWhiteSpace(tokens.Issuer) || tokens.Issuer.Trim().Length < 3)
                errors.Add("Token.Issuer must be at least 3 characters when IssueJwt is enabled.");

            if (string.IsNullOrWhiteSpace(tokens.Audience) || tokens.Audience.Trim().Length < 3)
                errors.Add("Token.Audience must be at least 3 characters when IssueJwt is enabled.");
        }

        if (tokens.IssueOpaque)
        {
            if (tokens.OpaqueIdBytes < 16)
                errors.Add("Token.OpaqueIdBytes must be at least 16 bytes (128-bit entropy).");

            if (tokens.OpaqueIdBytes > 128)
                errors.Add("Token.OpaqueIdBytes must not exceed 64 bytes.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
