using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Server.Services
{
    internal sealed class UAuthSessionService<TUserId> : IUAuthSessionService<TUserId>
    {
        private readonly ISessionOrchestrator<TUserId> _orchestrator;

        public UAuthSessionService(ISessionOrchestrator<TUserId> orchestrator)
        {
            _orchestrator = orchestrator;
        }

        public Task<SessionValidationResult<TUserId>> ValidateSessionAsync(
            string? tenantId,
            AuthSessionId sessionId,
            DateTime now)
        {
            var context = new SessionValidationContext()
            {
                TenantId = tenantId,
                Now = now
            };

            return _orchestrator.ValidateSessionAsync(context);
        }

        public Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsAsync(
            string? tenantId,
            TUserId userId)
            => _orchestrator.GetChainsAsync(
                tenantId,
                userId);

        public Task<IReadOnlyList<ISession<TUserId>>> GetSessionsAsync(
            string? tenantId,
            ChainId chainId)
            => _orchestrator.GetSessionsAsync(
                tenantId,
                chainId);

        public Task<ISession<TUserId>?> GetSessionAsync(
            string? tenantId,
            AuthSessionId sessionId)
            => _orchestrator.GetSessionAsync(
                tenantId,
                sessionId);

        public Task RevokeSessionAsync(
            string? tenantId,
            AuthSessionId sessionId,
            DateTime at)
            => _orchestrator.RevokeSessionAsync(
                tenantId,
                sessionId,
                at);

        public Task<ChainId?> ResolveChainIdAsync(
            string? tenantId,
            AuthSessionId sessionId)
            => _orchestrator.ResolveChainIdAsync(tenantId, sessionId);

        public Task RevokeAllChainsAsync(
            string? tenantId,
            TUserId userId,
            ChainId? exceptChainId,
            DateTime at)
            => _orchestrator.RevokeAllChainsAsync(
                tenantId,
                userId,
                exceptChainId,
                at);

        public Task RevokeChainAsync(
            string? tenantId,
            ChainId chainId,
            DateTime at)
            => _orchestrator.RevokeChainAsync(
                tenantId,
                chainId,
                at);

        public Task RevokeRootAsync(
            string? tenantId,
            TUserId userId,
            DateTime at)
            => _orchestrator.RevokeRootAsync(
                tenantId,
                userId,
                at);

        public Task<ISession<TUserId>?> GetCurrentSessionAsync(string? tenantId, AuthSessionId sessionId)
        {
            // TODO: Implement this method
            throw new NotImplementedException();
        }

        public Task<IssuedSession<TUserId>> IssueSessionAfterAuthenticationAsync(
            string? tenantId,
            AuthenticatedSessionContext<TUserId> context,
            CancellationToken cancellationToken = default)
        {
            if (context.UserId is null)
                throw new InvalidOperationException(
                    "Authenticated session context requires a valid user id.");

            // Authenticated → IssueContext map
            var issueContext = new AuthenticatedSessionContext<TUserId>
            {
                TenantId = tenantId,
                UserId = context.UserId,
                Now = context.Now,
                DeviceInfo = context.DeviceInfo,
                Claims = context.Claims,
                ChainId = context.ChainId
            };

            return _orchestrator.CreateLoginSessionAsync(issueContext);
        }

    }
}
