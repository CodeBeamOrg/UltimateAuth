using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthServerPasswordPolicyOptionsValidator : IValidateOptions<UAuthServerOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthServerOptions options)
    {
        var errors = new List<string>();

        if (options is null)
        {
            return ValidateOptionsResult.Fail("Password policy options cannot be null.");
        }

        if (options.Password.Level is not (
            PasswordPolicyLevel.Basic or
            PasswordPolicyLevel.Standard or
            PasswordPolicyLevel.Strict))
        {
            errors.Add("Password policy Level must be Basic, Standard or Strict.");
        }

        if (!IsValidCheckLevel(options.Password.CommonPasswords))
        {
            errors.Add("Invalid CommonPasswords level.");
        }

        if (!IsValidCheckLevel(options.Password.UserSimilarity))
        {
            errors.Add("Invalid UserSimilarity level.");
        }

        if (options.Password.History is null)
        {
            errors.Add("Password History options cannot be null.");
        }
        else if (options.Password.History.RememberLast < 0 || options.Password.History.RememberLast > PasswordPolicyLimits.MaxHistoryCount)
        {
            errors.Add("Password history count must be between 0 and 10.");
        }

        if (errors.Count > 0)
            return ValidateOptionsResult.Fail(errors);

        var effective = PasswordPolicyOptionsResolver.Resolve(options.Password);

        if (effective.MinLength < PasswordPolicyLimits.MinimumLength)
        {
            errors.Add($"Password MinLength must be at least " + $"{PasswordPolicyLimits.MinimumLength}.");
        }

        if (effective.MaxLength > PasswordPolicyLimits.AbsoluteMaxLength)
        {
            errors.Add($"Password MaxLength cannot exceed " + $"{PasswordPolicyLimits.AbsoluteMaxLength}.");
        }

        if (effective.MaxLength < effective.MinLength)
        {
            errors.Add("Password MaxLength cannot be smaller than MinLength.");
        }

        if (effective.MaxLength <= 0)
        {
            errors.Add("Password MaxLength must be positive.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    private static bool IsValidCheckLevel(PasswordPolicyLevel? level)
    {
        return level is null or
            PasswordPolicyLevel.Disabled or
            PasswordPolicyLevel.Basic or
            PasswordPolicyLevel.Standard or
            PasswordPolicyLevel.Strict;
    }
}
