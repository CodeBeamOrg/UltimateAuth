using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Infrastructure
{
    public sealed class UAuthRefreshTokenResolver<TUserId> : IRefreshTokenResolver<TUserId>
    {
        private readonly ISessionStoreFactory _sessionStoreFactory;
        private readonly ITokenStoreFactory _tokenStoreFactory;
        private readonly ITokenHasher _hasher;

        public UAuthRefreshTokenResolver(ISessionStoreFactory sessionStoreFactory, ITokenStoreFactory tokenStoreFactory, ITokenHasher hasher)
        {
            _sessionStoreFactory = sessionStoreFactory;
            _tokenStoreFactory = tokenStoreFactory;
            _hasher = hasher;
        }

        public async Task<ResolvedRefreshSession<TUserId>?> ResolveAsync(string? tenantId, string refreshToken, DateTimeOffset now, CancellationToken ct = default)
        {
            var tokenHash = _hasher.Hash(refreshToken);

            var tokenStore = _tokenStoreFactory.Create(tenantId);
            var sessionStore = _sessionStoreFactory.Create<TUserId>(tenantId);

            var stored = await tokenStore.GetRefreshTokenAsync(
                tenantId,
                tokenHash);

            if (stored is null)
                return null;

            if (stored.IsRevoked)
            {
                return ResolvedRefreshSession<TUserId>.Reused();
            }

            if (stored.ExpiresAt <= now)
            {
                await tokenStore.RevokeRefreshTokenAsync(
                    tenantId,
                    tokenHash,
                    now);

                return ResolvedRefreshSession<TUserId>.Invalid();
            }

            var session = await sessionStore.GetSessionAsync(
                tenantId,
                stored.SessionId);

            if (session is null)
                return null;

            if (session.IsRevoked || session.ExpiresAt <= now)
                return null;

            var chain = await sessionStore.GetChainAsync(
                tenantId,
                session.ChainId);

            if (chain is null || chain.IsRevoked)
                return null;

            await tokenStore.RevokeRefreshTokenAsync(
                tenantId,
                tokenHash,
                now);

            return ResolvedRefreshSession<TUserId>.Valid(
                session,
                chain);
        }
    }
}
