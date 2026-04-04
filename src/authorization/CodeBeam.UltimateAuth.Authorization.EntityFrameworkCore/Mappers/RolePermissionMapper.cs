using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;

internal static class RolePermissionMapper
{
    public static RolePermissionProjection ToProjection(TenantKey tenant, RoleId roleId, Permission permission)
    {
        return new RolePermissionProjection
        {
            Tenant = tenant,
            RoleId = roleId,
            Permission = permission.Value
        };
    }

    public static Permission ToDomain(RolePermissionProjection projection)
    {
        return Permission.From(projection.Permission);
    }
}
