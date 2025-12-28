using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore
{
    internal static class SessionProjectionMapper
    {
        public static ISession<TUserId> ToDomain<TUserId>(this SessionProjection<TUserId> p)
        {
            var device = p.Device == DeviceInfo.Empty
                ? DeviceInfo.Unknown
                : p.Device;

            return UAuthSession<TUserId>.FromProjection(
                p.SessionId,
                p.TenantId,
                p.UserId,
                p.ChainId,
                p.CreatedAt,
                p.ExpiresAt,
                p.LastSeenAt,
                p.IsRevoked,
                p.RevokedAt,
                p.SecurityVersionAtCreation,
                device,
                p.Claims,
                p.Metadata
            );
        }

        public static SessionProjection<TUserId> ToProjection<TUserId>(this ISession<TUserId> s)
        {
            return new SessionProjection<TUserId>
            {
                SessionId = s.SessionId,
                TenantId = s.TenantId,
                UserId = s.UserId,
                ChainId = s.ChainId,

                CreatedAt = s.CreatedAt,
                ExpiresAt = s.ExpiresAt,
                LastSeenAt = s.LastSeenAt,

                IsRevoked = s.IsRevoked,
                RevokedAt = s.RevokedAt,

                SecurityVersionAtCreation = s.SecurityVersionAtCreation,
                Device = s.Device,
                Claims = s.Claims,
                Metadata = s.Metadata
            };
        }

    }
}
