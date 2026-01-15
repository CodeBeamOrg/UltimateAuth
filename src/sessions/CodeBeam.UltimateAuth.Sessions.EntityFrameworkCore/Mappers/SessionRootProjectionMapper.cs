using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore
{
    internal static class SessionRootProjectionMapper
    {
        public static ISessionRoot ToDomain(this SessionRootProjection root, IReadOnlyList<ISessionChain>? chains = null)
        {
            return UAuthSessionRoot.FromProjection(
                root.RootId,
                root.TenantId,
                root.UserKey,
                root.IsRevoked,
                root.RevokedAt,
                root.SecurityVersion,
                chains ?? Array.Empty<ISessionChain>(),
                root.LastUpdatedAt
            );
        }

        public static SessionRootProjection ToProjection(this ISessionRoot root)
        {
            return new SessionRootProjection
            {
                RootId = root.RootId,
                TenantId = root.TenantId,
                UserKey = root.UserKey,

                IsRevoked = root.IsRevoked,
                RevokedAt = root.RevokedAt,

                SecurityVersion = root.SecurityVersion,
                LastUpdatedAt = root.LastUpdatedAt
            };
        }

    }
}
