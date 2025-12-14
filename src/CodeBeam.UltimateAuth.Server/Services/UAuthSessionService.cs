using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Models;
using CodeBeam.UltimateAuth.Server.Sessions;

namespace CodeBeam.UltimateAuth.Server.Services
{
    internal sealed class UAuthSessionService<TUserId> : IUAuthSessionService<TUserId>
    {
        private readonly ISessionOrchestrator<TUserId> _engine;

        public UAuthSessionService(
            ISessionOrchestrator<TUserId> engine)
        {
            _engine = engine;
        }

        public Task<SessionValidationResult<TUserId>> ValidateSessionAsync(
            string? tenantId,
            AuthSessionId sessionId,
            DateTime now)
            => _engine.ValidateSessionAsync(tenantId, sessionId, now);

        public Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsAsync(
            string? tenantId,
            TUserId userId)
            => _engine.GetChainsAsync(tenantId, userId);

        public Task<IReadOnlyList<ISession<TUserId>>> GetSessionsAsync(
            string? tenantId,
            ChainId chainId)
            => _engine.GetSessionsAsync(tenantId, chainId);

        public Task<ISession<TUserId>?> GetCurrentSessionAsync(
            string? tenantId,
            AuthSessionId sessionId)
            => _engine.GetSessionAsync(tenantId, sessionId);

        public Task RevokeSessionAsync(
            string? tenantId,
            AuthSessionId sessionId,
            DateTime at)
            => _engine.RevokeSessionAsync(tenantId, sessionId, at);

        public Task RevokeChainAsync(
            string? tenantId,
            ChainId chainId,
            DateTime at)
            => _engine.RevokeChainAsync(tenantId, chainId, at);

        public Task RevokeRootAsync(
            string? tenantId,
            TUserId userId,
            DateTime at)
            => _engine.RevokeRootAsync(tenantId, userId, at);
    }

}
