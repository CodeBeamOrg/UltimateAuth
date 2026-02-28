using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

internal static class SessionChainProjectionMapper
{
    public static UAuthSessionChain ToDomain(this SessionChainProjection p)
    {
        return UAuthSessionChain.FromProjection(
            p.ChainId,
            p.RootId,
            p.Tenant,
            p.UserKey,
            p.CreatedAt,
            p.LastSeenAt,
            p.AbsoluteExpiresAt,
            p.Device,
            p.ClaimsSnapshot,
            p.ActiveSessionId,
            p.RotationCount,
            p.TouchCount,
            p.SecurityVersionAtCreation,
            p.IsRevoked,
            p.RevokedAt,
            p.Version
        );
    }

    public static SessionChainProjection ToProjection(this UAuthSessionChain chain)
    {
        return new SessionChainProjection
        {
            ChainId = chain.ChainId,
            RootId = chain.RootId,
            Tenant = chain.Tenant,
            UserKey = chain.UserKey,
            CreatedAt = chain.CreatedAt,
            LastSeenAt = chain.LastSeenAt,
            AbsoluteExpiresAt = chain.AbsoluteExpiresAt,
            Device = chain.Device,
            ClaimsSnapshot = chain.ClaimsSnapshot,
            ActiveSessionId = chain.ActiveSessionId,
            RotationCount = chain.RotationCount,
            TouchCount = chain.TouchCount,
            SecurityVersionAtCreation = chain.SecurityVersionAtCreation,
            IsRevoked = chain.IsRevoked,
            RevokedAt = chain.RevokedAt,
            Version = chain.Version
        };
    }

}
