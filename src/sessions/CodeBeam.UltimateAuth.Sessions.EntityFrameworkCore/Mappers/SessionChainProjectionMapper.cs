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
            p.RotationCount,
            p.SecurityVersionAtCreation,
            p.ClaimsSnapshot,
            p.ActiveSessionId,
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
            Tenant = chain.Tenant,
            UserKey = chain.UserKey,

            RotationCount = chain.RotationCount,
            SecurityVersionAtCreation = chain.SecurityVersionAtCreation,
            ClaimsSnapshot = chain.ClaimsSnapshot,

            ActiveSessionId = chain.ActiveSessionId,

            IsRevoked = chain.IsRevoked,
            RevokedAt = chain.RevokedAt,
            Version = chain.Version
        };
    }

}
