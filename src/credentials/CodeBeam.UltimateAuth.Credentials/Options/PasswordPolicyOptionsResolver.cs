using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Options;

public static class PasswordPolicyOptionsResolver
{
    public static EffectivePasswordPolicyOptions Resolve(PasswordPolicyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var preset = options.Level switch
        {
            PasswordPolicyLevel.Basic => CreateBasic(),
            PasswordPolicyLevel.Standard => CreateStandard(),
            PasswordPolicyLevel.Strict => CreateStrict(),

            _ => throw new ArgumentOutOfRangeException(nameof(options), "Invalid password policy level.")
        };

        return preset with
        {
            MinLength = options.MinLength ?? preset.MinLength,
            MaxLength = options.MaxLength ?? preset.MaxLength,

            RequireLetter = options.RequireLetter ?? preset.RequireLetter,
            RequireDigit = options.RequireDigit ?? preset.RequireDigit,
            RequireUppercase = options.RequireUppercase ?? preset.RequireUppercase,
            RequireLowercase = options.RequireLowercase ?? preset.RequireLowercase,
            RequireSpecialCharacter = options.RequireSpecialCharacter ?? preset.RequireSpecialCharacter,
            CommonPasswords = options.CommonPasswords ?? preset.CommonPasswords,
            UserSimilarity = options.UserSimilarity ?? preset.UserSimilarity,
            RememberLast = options.History?.RememberLast ?? 0
        };
    }

    private static EffectivePasswordPolicyOptions CreateBasic() =>
        Create(
            PasswordPolicyLevel.Basic,
            minLength: 8,
            requireLetter: false,
            requireDigit: false,
            requireUppercase: false,
            requireLowercase: false,
            requireSpecialCharacter: false);

    private static EffectivePasswordPolicyOptions CreateStandard() =>
        Create(
            PasswordPolicyLevel.Standard,
            minLength: 8,
            requireLetter: true,
            requireDigit: true,
            requireUppercase: false,
            requireLowercase: false,
            requireSpecialCharacter: false);

    private static EffectivePasswordPolicyOptions CreateStrict() =>
        Create(
            PasswordPolicyLevel.Strict,
            minLength: 15,
            requireLetter: true,
            requireDigit: true,
            requireUppercase: true,
            requireLowercase: true,
            requireSpecialCharacter: true);

    private static EffectivePasswordPolicyOptions Create(
        PasswordPolicyLevel level,
        int minLength,
        bool requireLetter,
        bool requireDigit,
        bool requireUppercase,
        bool requireLowercase,
        bool requireSpecialCharacter)
    {
        return new EffectivePasswordPolicyOptions
        {
            Level = level,
            MinLength = minLength,
            MaxLength = PasswordPolicyLimits.DefaultMaxLength,

            RequireLetter = requireLetter,
            RequireDigit = requireDigit,
            RequireUppercase = requireUppercase,
            RequireLowercase = requireLowercase,
            RequireSpecialCharacter = requireSpecialCharacter,

            CommonPasswords = level,
            UserSimilarity = level,

            RememberLast = 0
        };
    }
}
