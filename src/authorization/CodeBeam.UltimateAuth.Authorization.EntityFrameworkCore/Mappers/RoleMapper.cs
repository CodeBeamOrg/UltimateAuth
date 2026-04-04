using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;

internal static class RoleMapper
{
    public static Role ToDomain(RoleProjection projection, IEnumerable<RolePermissionProjection> permissionEntities)
    {
        var permissions = permissionEntities.Select(RolePermissionMapper.ToDomain);

        return Role.FromProjection(
            projection.Id,
            projection.Tenant,
            projection.Name,
            permissions,
            projection.CreatedAt,
            projection.UpdatedAt,
            projection.DeletedAt,
            projection.Version);
    }

    public static RoleProjection ToProjection(Role role)
    {
        return new RoleProjection
        {
            Id = role.Id,
            Tenant = role.Tenant,
            Name = role.Name,
            NormalizedName = role.NormalizedName,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt,
            DeletedAt = role.DeletedAt,
            Version = role.Version
        };
    }

    public static void UpdateProjection(Role role, RoleProjection entity)
    {
        entity.Name = role.Name;
        entity.NormalizedName = role.NormalizedName;
        entity.UpdatedAt = role.UpdatedAt;
    }
}