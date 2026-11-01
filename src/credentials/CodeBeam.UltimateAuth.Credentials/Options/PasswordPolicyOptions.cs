using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Options;

public sealed class PasswordPolicyOptions
{
    public PasswordPolicyLevel Level { get; set; } = PasswordPolicyLevel.Standard;

    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }

    public bool? RequireLetter { get; set; }
    public bool? RequireDigit { get; set; }
    public bool? RequireUppercase { get; set; }
    public bool? RequireLowercase { get; set; }
    public bool? RequireSpecialCharacter { get; set; }

    public PasswordPolicyLevel? CommonPasswords { get; set; }
    public PasswordPolicyLevel? UserSimilarity { get; set; }

    public PasswordHistoryOptions History { get; set; } = new();

    public PasswordPolicyOptions Clone() => new()
    {
        Level = Level,
        MinLength = MinLength,
        MaxLength = MaxLength,
        RequireLetter = RequireLetter,
        RequireDigit = RequireDigit,
        RequireUppercase = RequireUppercase,
        RequireLowercase = RequireLowercase,
        RequireSpecialCharacter = RequireSpecialCharacter,
        CommonPasswords = CommonPasswords,
        UserSimilarity = UserSimilarity,
        History = History.Clone()
    };
}
