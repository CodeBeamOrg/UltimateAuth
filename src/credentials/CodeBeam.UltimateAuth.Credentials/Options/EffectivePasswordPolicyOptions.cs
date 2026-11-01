using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Options;

public sealed record EffectivePasswordPolicyOptions
{
    public required PasswordPolicyLevel Level { get; init; }

    public required int MinLength { get; init; }
    public required int MaxLength { get; init; }

    public required bool RequireLetter { get; init; }
    public required bool RequireDigit { get; init; }
    public required bool RequireUppercase { get; init; }
    public required bool RequireLowercase { get; init; }
    public required bool RequireSpecialCharacter { get; init; }

    public required PasswordPolicyLevel CommonPasswords { get; init; }
    public required PasswordPolicyLevel UserSimilarity { get; init; }

    public required int RememberLast { get; init; }
}
