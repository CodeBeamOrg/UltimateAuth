using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal static class UserLifecycleMapper
{
    public static UserLifecycle ToDomain(this UserLifecycleProjection p)
    {
        return UserLifecycle.FromProjection(
            p.Id,
            p.Tenant,
            p.UserKey,
            p.Status,
            p.SecurityVersion,
            p.CreatedAt,
            p.UpdatedAt,
            p.DeletedAt,
            p.Version);
    }

    public static UserLifecycleProjection ToProjection(this UserLifecycle d)
    {
        return new UserLifecycleProjection
        {
            Id = d.Id,
            Tenant = d.Tenant,
            UserKey = d.UserKey,
            Status = d.Status,
            SecurityVersion = d.SecurityVersion,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt,
            DeletedAt = d.DeletedAt,
            Version = d.Version
        };
    }

    public static void UpdateProjection(this UserLifecycle source, UserLifecycleProjection target)
    {
        target.Status = source.Status;
        target.SecurityVersion = source.SecurityVersion;
        target.UpdatedAt = source.UpdatedAt;
        target.DeletedAt = source.DeletedAt;
    }
}
