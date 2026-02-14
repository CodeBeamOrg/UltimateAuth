using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record AuthIdentity
{
    public required UserKey UserKey { get; init; }
    public required TenantKey Tenant { get; init; }
    public string? UserName { get; init; }
    public DateTimeOffset? AuthenticatedAt { get; init; }
}
