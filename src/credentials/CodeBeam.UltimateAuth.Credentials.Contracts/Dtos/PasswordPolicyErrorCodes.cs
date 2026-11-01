namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public static class PasswordPolicyErrorCodes
{
    public const string Required = "password_required";
    public const string TooShort = "password_too_short";
    public const string TooLong = "password_too_long";

    public const string LetterRequired = "password_letter_required";
    public const string DigitRequired = "password_digit_required";
    public const string UppercaseRequired = "password_uppercase_required";
    public const string LowercaseRequired = "password_lowercase_required";
    public const string SpecialCharacterRequired = "password_special_character_required";

    public const string CommonPassword = "password_too_common";
    public const string SimilarToUser = "password_similar_to_user";

    public const string ReusedPassword = "password_reused";
    public const string InputTooLong = "password_input_too_long";
}
