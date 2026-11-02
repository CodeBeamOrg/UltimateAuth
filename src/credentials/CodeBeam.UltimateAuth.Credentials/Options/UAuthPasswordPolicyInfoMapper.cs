using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Options;

public static class UAuthPasswordPolicyInfoMapper
{
    public static UAuthPasswordPolicyInfo ToInfo(this EffectivePasswordPolicyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new UAuthPasswordPolicyInfo
        {
            Level = options.Level,

            MinLength = options.MinLength,
            MaxLength = options.MaxLength,

            RequireLetter = options.RequireLetter,
            RequireDigit = options.RequireDigit,
            RequireUppercase = options.RequireUppercase,
            RequireLowercase = options.RequireLowercase,
            RequireSpecialCharacter = options.RequireSpecialCharacter,

            CheckCommonPasswords = options.CommonPasswords != PasswordPolicyLevel.Disabled,
            CheckUserSimilarity = options.UserSimilarity != PasswordPolicyLevel.Disabled,
            CheckPasswordHistory = options.RememberLast > 0
        };
    }
}
