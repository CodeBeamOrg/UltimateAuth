using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Client;
public static class UAuthPasswordClientValidator
{
    public static IReadOnlyList<string> Validate(string? password, UAuthPasswordPolicyInfo policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var errors = new List<string>();

        if (string.IsNullOrEmpty(password))
        {
            errors.Add(PasswordPolicyErrorCodes.Required);
            return errors;
        }

        if (password.Length > 256)
        {
            errors.Add(PasswordPolicyErrorCodes.InputTooLong);
            return errors;
        }

        if (password.Length < policy.MinLength)
            errors.Add(PasswordPolicyErrorCodes.TooShort);

        if (password.Length > policy.MaxLength)
            errors.Add(PasswordPolicyErrorCodes.TooLong);

        if (policy.RequireLetter &&
            !password.Any(char.IsLetter))
            errors.Add(PasswordPolicyErrorCodes.LetterRequired);

        if (policy.RequireDigit &&
            !password.Any(char.IsDigit))
            errors.Add(PasswordPolicyErrorCodes.DigitRequired);

        if (policy.RequireUppercase &&
            !password.Any(char.IsUpper))
            errors.Add(PasswordPolicyErrorCodes.UppercaseRequired);

        if (policy.RequireLowercase &&
            !password.Any(char.IsLower))
            errors.Add(PasswordPolicyErrorCodes.LowercaseRequired);

        if (policy.RequireSpecialCharacter &&
            !password.Any(c => !char.IsLetterOrDigit(c)))
            errors.Add(PasswordPolicyErrorCodes.SpecialCharacterRequired);

        return errors;
    }
}
