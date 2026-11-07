using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.IdentityLab.Contracts;

public static class LabPasswordErrorMessages
{
    public static string Get(string? code) => code switch
    {
        PasswordPolicyErrorCodes.TooShort =>
            "Your password is too short.",

        PasswordPolicyErrorCodes.TooLong or
        PasswordPolicyErrorCodes.InputTooLong =>
            "Your password exceeds the allowed length.",

        PasswordPolicyErrorCodes.LetterRequired =>
            "Your password must contain a letter.",

        PasswordPolicyErrorCodes.DigitRequired =>
            "Your password must contain a number.",

        PasswordPolicyErrorCodes.UppercaseRequired =>
            "Your password must contain an uppercase letter.",

        PasswordPolicyErrorCodes.LowercaseRequired =>
            "Your password must contain a lowercase letter.",

        PasswordPolicyErrorCodes.SpecialCharacterRequired =>
            "Your password must contain a special character.",

        PasswordPolicyErrorCodes.CommonPassword =>
            "This password is too common. Please choose a less predictable password.",

        PasswordPolicyErrorCodes.SimilarToUser =>
            "Your password is too similar to your username or email address.",

        PasswordPolicyErrorCodes.ReusedPassword =>
            "You cannot reuse a previous password.",

        _ => "Your password does not meet the security requirements."
    };
}
