using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record PkceAuthorizeCommand
{
    public string CodeChallenge { get; init; } = default!;
    public string ChallengeMethod { get; init; } = "S256";
    public string? DeviceId { get; init; }
    public string? RedirectUri { get; init; }

    public UAuthClientProfile ClientProfile { get; init; }
    public TenantKey Tenant { get; init; }
}
