using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore
{
    internal static class SessionChainProjectionMapper
    {
        public static ISessionChain ToDomain(this SessionChainProjection p)
        {
            return UAuthSessionChain.FromProjection(
                p.ChainId,
                p.RootId,
                p.TenantId,
                p.UserKey,
                p.RotationCount,
                p.SecurityVersionAtCreation,
                p.ClaimsSnapshot,
                p.ActiveSessionId,
                p.IsRevoked,
                p.RevokedAt
            );
        }

        public static SessionChainProjection ToProjection(this ISessionChain chain)
        {
            return new SessionChainProjection
            {
                ChainId = chain.ChainId,
                TenantId = chain.TenantId,
                UserKey = chain.UserKey,

                RotationCount = chain.RotationCount,
                SecurityVersionAtCreation = chain.SecurityVersionAtCreation,
                ClaimsSnapshot = chain.ClaimsSnapshot,

                ActiveSessionId = chain.ActiveSessionId,

                IsRevoked = chain.IsRevoked,
                RevokedAt = chain.RevokedAt
            };
        }

    }
}
