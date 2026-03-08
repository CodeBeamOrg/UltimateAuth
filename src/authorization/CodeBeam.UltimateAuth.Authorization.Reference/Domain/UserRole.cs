using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Authorization.Domain;

namespace CodeBeam.UltimateAuth.Authorization.Reference;

public sealed class UserRole
{
    public TenantKey Tenant { get; init; }
    public UserKey UserKey { get; init; }

    public RoleId RoleId { get; init; }

    public DateTimeOffset AssignedAt { get; init; }
}