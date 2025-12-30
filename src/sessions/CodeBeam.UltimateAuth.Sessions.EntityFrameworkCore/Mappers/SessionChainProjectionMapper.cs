using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore
{
    internal static class SessionChainProjectionMapper
    {
        public static ISessionChain<TUserId> ToDomain<TUserId>(this SessionChainProjection<TUserId> p)
        {
            return UAuthSessionChain<TUserId>.FromProjection(
                p.ChainId,
                p.TenantId,
                p.UserId,
                p.RotationCount,
                p.SecurityVersionAtCreation,
                p.ClaimsSnapshot,
                p.ActiveSessionId,
                p.IsRevoked,
                p.RevokedAt
            );
        }

        public static SessionChainProjection<TUserId> ToProjection<TUserId>(this ISessionChain<TUserId> chain)
        {
            return new SessionChainProjection<TUserId>
            {
                ChainId = chain.ChainId,
                TenantId = chain.TenantId,
                UserId = chain.UserId,

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
