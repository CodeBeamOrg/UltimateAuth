namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record UAuthPasswordPolicyInfo
{
    public required PasswordPolicyLevel Level { get; init; }

    public required int MinLength { get; init; }
    public required int MaxLength { get; init; }

    public required bool RequireLetter { get; init; }
    public required bool RequireDigit { get; init; }
    public required bool RequireUppercase { get; init; }
    public required bool RequireLowercase { get; init; }
    public required bool RequireSpecialCharacter { get; init; }

    public required bool CheckCommonPasswords { get; init; }
    public required bool CheckUserSimilarity { get; init; }
    public required bool CheckPasswordHistory { get; init; }
}
