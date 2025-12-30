using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore
{
    internal static class SessionRootProjectionMapper
    {
        public static ISessionRoot<TUserId> ToDomain<TUserId>(this SessionRootProjection<TUserId> root, IReadOnlyList<ISessionChain<TUserId>> chains)
        {
            return UAuthSessionRoot<TUserId>.FromProjection(
                root.TenantId,
                root.UserId,
                root.IsRevoked,
                root.RevokedAt,
                root.SecurityVersion,
                chains,
                root.LastUpdatedAt
            );
        }

        public static SessionRootProjection<TUserId> ToProjection<TUserId>(this ISessionRoot<TUserId> root)
        {
            return new SessionRootProjection<TUserId>
            {
                TenantId = root.TenantId,
                UserId = root.UserId,

                IsRevoked = root.IsRevoked,
                RevokedAt = root.RevokedAt,

                SecurityVersion = root.SecurityVersion,
                LastUpdatedAt = root.LastUpdatedAt
            };
        }

    }
}
