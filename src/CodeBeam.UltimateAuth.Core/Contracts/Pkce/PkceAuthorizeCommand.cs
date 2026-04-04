using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record PkceAuthorizeCommand
{
    public required string CodeChallenge { get; init; }
    public required string ChallengeMethod { get; init; } = "S256";
    public required DeviceContext Device { get; init; }
    public string? RedirectUri { get; init; }

    public UAuthClientProfile ClientProfile { get; init; }
    public TenantKey Tenant { get; init; }
}
