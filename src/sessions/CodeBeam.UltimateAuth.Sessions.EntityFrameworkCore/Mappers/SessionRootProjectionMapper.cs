using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

internal static class SessionRootProjectionMapper
{
    public static UAuthSessionRoot ToDomain(this SessionRootProjection root, IReadOnlyList<UAuthSessionChain>? chains = null)
    {
        return UAuthSessionRoot.FromProjection(
            root.RootId,
            root.Tenant,
            root.UserKey,
            root.IsRevoked,
            root.RevokedAt,
            root.SecurityVersion,
            chains ?? Array.Empty<UAuthSessionChain>(),
            root.LastUpdatedAt,
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

            IsRevoked = root.IsRevoked,
            RevokedAt = root.RevokedAt,

            SecurityVersion = root.SecurityVersion,
            LastUpdatedAt = root.LastUpdatedAt,
            Version = root.Version
        };
    }
}
