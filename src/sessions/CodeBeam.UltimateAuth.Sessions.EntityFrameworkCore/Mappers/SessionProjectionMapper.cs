using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

internal static class SessionProjectionMapper
{
    public static UAuthSession ToDomain(this SessionProjection p)
    {
        return UAuthSession.FromProjection(
            p.SessionId,
            p.Tenant,
            p.UserKey,
            p.ChainId,
            p.CreatedAt,
            p.ExpiresAt,
            p.IsRevoked,
            p.RevokedAt,
            p.SecurityVersionAtCreation,
            p.Claims,
            p.Metadata,
            p.Version
        );
    }

    public static SessionProjection ToProjection(this UAuthSession s)
    {
        return new SessionProjection
        {
            SessionId = s.SessionId,
            Tenant = s.Tenant,
            UserKey = s.UserKey,
            ChainId = s.ChainId,

            CreatedAt = s.CreatedAt,
            ExpiresAt = s.ExpiresAt,

            IsRevoked = s.IsRevoked,
            RevokedAt = s.RevokedAt,

            SecurityVersionAtCreation = s.SecurityVersionAtCreation,
            Claims = s.Claims,
            Metadata = s.Metadata,
            Version = s.Version
        };
    }

}
