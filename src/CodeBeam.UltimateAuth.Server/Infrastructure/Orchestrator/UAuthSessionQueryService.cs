using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class UAuthSessionQueryService<TUserId> : ISessionQueryService<TUserId>
    {
        private readonly ISessionStoreFactory _storeFactory;

        public UAuthSessionQueryService(ISessionStoreFactory storeFactory)
        {
            _storeFactory = storeFactory;
        }

        public async Task<SessionValidationResult<TUserId>> ValidateSessionAsync(
            SessionValidationContext context,
            CancellationToken ct = default)
        {
            var kernel = _storeFactory.Create<TUserId>(context.TenantId);

            var session = await kernel.GetSessionAsync(
                context.TenantId,
                context.SessionId);

            if (session is null)
                return SessionValidationResult<TUserId>.Invalid(SessionState.NotFound);

            var state = session.GetState(context.Now);
            if (state != SessionState.Active)
                return SessionValidationResult<TUserId>.Invalid(state);

            var chain = await kernel.GetChainAsync(
                context.TenantId,
                session.ChainId);

            if (chain is null || chain.IsRevoked)
                return SessionValidationResult<TUserId>.Invalid(SessionState.Revoked);

            var root = await kernel.GetSessionRootAsync(
                context.TenantId,
                session.UserId);

            if (root is null || root.IsRevoked)
                return SessionValidationResult<TUserId>.Invalid(SessionState.Revoked);

            if (session.SecurityVersionAtCreation != root.SecurityVersion)
                return SessionValidationResult<TUserId>.Invalid(SessionState.SecurityMismatch);

            if (!session.Device.Matches(context.Device))
                return SessionValidationResult<TUserId>.Invalid(SessionState.DeviceMismatch);

            if (session.ShouldUpdateLastSeen(context.Now))
            {
                var updated = session.Touch(context.Now);
                await kernel.SaveSessionAsync(context.TenantId, updated);
                session = updated;
            }

            return SessionValidationResult<TUserId>.Active(session, chain, root);
        }

        public Task<ISession<TUserId>?> GetSessionAsync(
            string? tenantId,
            AuthSessionId sessionId,
            CancellationToken ct = default)
        {
            var kernel = _storeFactory.Create<TUserId>(tenantId);
            return kernel.GetSessionAsync(tenantId, sessionId);
        }

        public Task<IReadOnlyList<ISession<TUserId>>> GetSessionsByChainAsync(
            string? tenantId,
            ChainId chainId,
            CancellationToken ct = default)
        {
            var kernel = _storeFactory.Create<TUserId>(tenantId);
            return kernel.GetSessionsByChainAsync(tenantId, chainId);
        }

        public Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsByUserAsync(
            string? tenantId,
            TUserId userId,
            CancellationToken ct = default)
        {
            var kernel = _storeFactory.Create<TUserId>(tenantId);
            return kernel.GetChainsByUserAsync(tenantId, userId);
        }

        public Task<ChainId?> ResolveChainIdAsync(
            string? tenantId,
            AuthSessionId sessionId,
            CancellationToken ct = default)
        {
            var kernel = _storeFactory.Create<TUserId>(tenantId);
            return kernel.GetChainIdBySessionAsync(tenantId, sessionId);
        }
    }

}
