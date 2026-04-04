using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;

public sealed class RolePermissionProjection
{
    public TenantKey Tenant { get; set; }

    public RoleId RoleId { get; set; }

    public string Permission { get; set; } = default!;
}
