using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthIdentifierNormalizationOptions
{
    public CaseHandling UsernameCase { get; set; } = CaseHandling.ToLower;
    public CaseHandling EmailCase { get; set; } = CaseHandling.ToLower;
    public CaseHandling CustomCase { get; set; } = CaseHandling.Preserve;

    internal UAuthIdentifierNormalizationOptions Clone() => new()
    {
        UsernameCase = UsernameCase,
        EmailCase = EmailCase,
        CustomCase = CustomCase
    };
}
