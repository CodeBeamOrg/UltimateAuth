using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;

internal sealed class UserRoleProjection
{
    public TenantKey Tenant { get; set; }

    public UserKey UserKey { get; set; }

    public RoleId RoleId { get; set; }

    public DateTimeOffset AssignedAt { get; set; }
}
