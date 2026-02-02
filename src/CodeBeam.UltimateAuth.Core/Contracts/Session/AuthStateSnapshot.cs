using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record AuthStateSnapshot
{
    public UserKey UserKey { get; init; }
    public TenantKey Tenant { get; init; }

    public ClaimsSnapshot Claims { get; init; } = ClaimsSnapshot.Empty;

    public DateTimeOffset? AuthenticatedAt { get; init; }
}
