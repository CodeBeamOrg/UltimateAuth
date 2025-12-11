using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Core.Options
{
    internal sealed class UAuthTokenOptionsValidator : IValidateOptions<UAuthTokenOptions>
    {
        public ValidateOptionsResult Validate(string? name, UAuthTokenOptions options)
        {
            var errors = new List<string>();

            if (!options.IssueJwt && !options.IssueOpaque)
                errors.Add("Token: At least one of IssueJwt or IssueOpaque must be enabled.");

            if (options.AccessTokenLifetime <= TimeSpan.Zero)
                errors.Add("Token.AccessTokenLifetime must be greater than zero.");

            if (options.RefreshTokenLifetime <= TimeSpan.Zero)
                errors.Add("Token.RefreshTokenLifetime must be greater than zero.");

            if (options.RefreshTokenLifetime <= options.AccessTokenLifetime)
                errors.Add("Token.RefreshTokenLifetime must be greater than Token.AccessTokenLifetime.");

            if (options.IssueJwt)
            {
                if (string.IsNullOrWhiteSpace(options.SigningKey))
                {
                    errors.Add("Token.SigningKey must not be empty when IssueJwt = true.");
                }
                else if (options.SigningKey.Length < 32) // 256-bit minimum
                {
                    errors.Add("Token.SigningKey must be at least 32 characters long (256-bit entropy).");
                }

                if (string.IsNullOrWhiteSpace(options.Issuer)) // TODO: Min 3 chars
                    errors.Add("Token.Issuer must not be empty when IssueJwt = true.");

                if (string.IsNullOrWhiteSpace(options.Audience))
                    errors.Add("Token.Audience must not be empty when IssueJwt = true.");
            }

            if (options.IssueOpaque)
            {
                if (options.OpaqueIdBytes < 16)
                    errors.Add("Token.OpaqueIdBytes must be at least 16 (128-bit entropy).");
            }

            return errors.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(errors);
        }
    }
}
