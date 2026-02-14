using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record AuthIdentitySnapshot
{
    public required UserKey UserKey { get; init; }
    public required TenantKey Tenant { get; init; }
    public string? PrimaryUserName { get; init; }
    public string? DisplayName { get; init; }
    public string? PrimaryEmail { get; init; }
    public string? PrimaryPhone { get; init; }
    public DateTimeOffset? AuthenticatedAt { get; init; }
}
