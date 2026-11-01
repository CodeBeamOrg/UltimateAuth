using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Options;

namespace CodeBeam.UltimateAuth.Credentials.Policies;

public sealed class DefaultPasswordStrengthPolicy(IPasswordCommonPasswordChecker commonPasswordChecker, IPasswordUserSimilarityChecker userSimilarityChecker) : IPasswordStrengthPolicy
{
    public async Task<PasswordPolicyResult> ValidateAsync(PasswordPolicyContext context, EffectivePasswordPolicyOptions options, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        ct.ThrowIfCancellationRequested();

        var violations = new List<PasswordPolicyViolation>();
        var password = context.Password;

        if (string.IsNullOrEmpty(password))
        {
            violations.Add(new(PasswordPolicyErrorCodes.Required, "Password is required."));

            return new PasswordPolicyResult(violations);
        }

        // Defensive check. The future pipeline will enforce
        // the absolute input limit independently of this policy.
        if (password.Length > PasswordPolicyLimits.AbsoluteMaxLength)
        {
            violations.Add(new(PasswordPolicyErrorCodes.InputTooLong, "Password exceeds the absolute length limit."));

            return new PasswordPolicyResult(violations);
        }

        if (password.Length < options.MinLength)
        {
            violations.Add(new(PasswordPolicyErrorCodes.TooShort, $"Password must contain at least {options.MinLength} characters."));
        }

        if (password.Length > options.MaxLength)
        {
            violations.Add(new(PasswordPolicyErrorCodes.TooLong, $"Password cannot exceed {options.MaxLength} characters."));
        }

        bool hasLetter = false;
        bool hasDigit = false;
        bool hasUppercase = false;
        bool hasLowercase = false;
        bool hasSpecialCharacter = false;

        foreach (var c in password)
        {
            if (char.IsLetter(c))
                hasLetter = true;

            if (char.IsDigit(c))
                hasDigit = true;

            if (char.IsUpper(c))
                hasUppercase = true;

            if (char.IsLower(c))
                hasLowercase = true;

            if (!char.IsLetterOrDigit(c))
                hasSpecialCharacter = true;
        }

        if (options.RequireLetter && !hasLetter)
        {
            violations.Add(new(PasswordPolicyErrorCodes.LetterRequired, "Password must contain a letter."));
        }

        if (options.RequireDigit && !hasDigit)
        {
            violations.Add(new(PasswordPolicyErrorCodes.DigitRequired, "Password must contain a digit."));
        }

        if (options.RequireUppercase && !hasUppercase)
        {
            violations.Add(new(PasswordPolicyErrorCodes.UppercaseRequired, "Password must contain an uppercase letter."));
        }

        if (options.RequireLowercase && !hasLowercase)
        {
            violations.Add(new(PasswordPolicyErrorCodes.LowercaseRequired, "Password must contain a lowercase letter."));
        }

        if (options.RequireSpecialCharacter && !hasSpecialCharacter)
        {
            violations.Add(new(PasswordPolicyErrorCodes.SpecialCharacterRequired, "Password must contain a special character."));
        }

        if (options.CommonPasswords != PasswordPolicyLevel.Disabled &&
            await commonPasswordChecker.IsCommonAsync(password, options.CommonPasswords, ct))
        {
            violations.Add(new(PasswordPolicyErrorCodes.CommonPassword, "Password is too common."));
        }

        if (options.UserSimilarity != PasswordPolicyLevel.Disabled && userSimilarityChecker.IsSimilar(context, options.UserSimilarity))
        {
            violations.Add(new(PasswordPolicyErrorCodes.SimilarToUser, "Password is too similar to user information."));
        }

        return new PasswordPolicyResult(violations);
    }
}
