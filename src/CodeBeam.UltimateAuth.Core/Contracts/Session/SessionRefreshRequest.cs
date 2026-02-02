using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record SessionRefreshRequest
{
    public TenantKey Tenant { get; init; }
    public string RefreshToken { get; init; } = default!;
}
