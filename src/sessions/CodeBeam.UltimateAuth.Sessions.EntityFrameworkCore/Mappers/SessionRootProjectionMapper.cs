using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

internal static class SessionRootProjectionMapper
{
    public static UAuthSessionRoot ToDomain(this SessionRootProjection root)
    {
        return UAuthSessionRoot.FromProjection(
            root.RootId,
            root.Tenant,
            root.UserKey,
            root.CreatedAt,
            root.UpdatedAt,
            root.IsRevoked,
            root.RevokedAt,
            root.SecurityVersion,
            root.Version
        );
    }

    public static SessionRootProjection ToProjection(this UAuthSessionRoot root)
    {
        return new SessionRootProjection
        {
            RootId = root.RootId,
            Tenant = root.Tenant,
            UserKey = root.UserKey,

            CreatedAt = root.CreatedAt,
            UpdatedAt = root.UpdatedAt,

            IsRevoked = root.IsRevoked,
            RevokedAt = root.RevokedAt,

            SecurityVersion = root.SecurityVersion,
            Version = root.Version
        };
    }
}
