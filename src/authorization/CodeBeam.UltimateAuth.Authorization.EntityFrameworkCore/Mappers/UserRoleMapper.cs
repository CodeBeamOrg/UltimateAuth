namespace CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore;

internal static class UserRoleMapper
{
    public static UserRole ToDomain(UserRoleProjection projection)
    {
        return new UserRole
        {
            Tenant = projection.Tenant,
            UserKey = projection.UserKey,
            RoleId = projection.RoleId,
            AssignedAt = projection.AssignedAt
        };
    }

    public static UserRoleProjection ToProjection(UserRole role)
    {
        return new UserRoleProjection
        {
            Tenant = role.Tenant,
            UserKey = role.UserKey,
            RoleId = role.RoleId,
            AssignedAt = role.AssignedAt
        };
    }
}
