using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Server.Options;
public sealed class UAuthLoginIdentifierOptions
{
    public ISet<UserIdentifierType> AllowedTypes { get; set; } =
        new HashSet<UserIdentifierType>
        {
            UserIdentifierType.Username,
            UserIdentifierType.Email,
            UserIdentifierType.Phone
        };

    public bool RequireVerificationForEmail { get; set; } = false;
    public bool RequireVerificationForPhone { get; set; } = false;

    public bool EnableCustomResolvers { get; set; } = true;
    public bool CustomResolversFirst { get; set; } = true;

    public UAuthIdentifierNormalizationOptions Normalization { get; set; } = new();

    public bool EnforceGlobalUniquenessForAllIdentifiers { get; set; } = false;

    internal UAuthLoginIdentifierOptions Clone() => new()
    {
        AllowedTypes = new HashSet<UserIdentifierType>(AllowedTypes),
        RequireVerificationForEmail = RequireVerificationForEmail,
        RequireVerificationForPhone = RequireVerificationForPhone,
        EnableCustomResolvers = EnableCustomResolvers,
        CustomResolversFirst = CustomResolversFirst,
        EnforceGlobalUniquenessForAllIdentifiers = EnforceGlobalUniquenessForAllIdentifiers,
        Normalization = Normalization.Clone()
    };
}

public sealed class UAuthIdentifierNormalizationOptions
{
    public CaseHandling UsernameCase { get; set; } = CaseHandling.Preserve;
    public CaseHandling EmailCase { get; set; } = CaseHandling.ToLower;
    public CaseHandling CustomCase { get; set; } = CaseHandling.Preserve;

    internal UAuthIdentifierNormalizationOptions Clone() => new()
    {
        UsernameCase = UsernameCase,
        EmailCase = EmailCase,
        CustomCase = CustomCase
    };
}
