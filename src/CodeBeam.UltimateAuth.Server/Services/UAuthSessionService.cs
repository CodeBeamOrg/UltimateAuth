using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Infrastructure.Orchestrator;

namespace CodeBeam.UltimateAuth.Server.Services
{
    internal sealed class UAuthSessionService<TUserId> : IUAuthSessionService<TUserId>
    {
        private readonly ISessionOrchestrator<TUserId> _orchestrator;
        private readonly ISessionQueryService<TUserId> _sessionQueryService;

        public UAuthSessionService(ISessionOrchestrator<TUserId> orchestrator, ISessionQueryService<TUserId> sessionQueryService)
        {
            _orchestrator = orchestrator;
            _sessionQueryService = sessionQueryService;
        }

        public Task<SessionValidationResult<TUserId>> ValidateSessionAsync(
            string? tenantId,
            AuthSessionId sessionId,
            DateTimeOffset now)
        {
            var context = new SessionValidationContext()
            {
                TenantId = tenantId,
                Now = now
            };

            return _sessionQueryService.ValidateSessionAsync(context);
        }

        public Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsAsync(
            string? tenantId,
            TUserId userId)
            => _sessionQueryService.GetChainsByUserAsync(
                tenantId,
                userId);

        public Task<IReadOnlyList<ISession<TUserId>>> GetSessionsAsync(
            string? tenantId,
            ChainId chainId)
            => _sessionQueryService.GetSessionsByChainAsync(
                tenantId,
                chainId);

        public Task<ISession<TUserId>?> GetSessionAsync(
            string? tenantId,
            AuthSessionId sessionId)
            => _sessionQueryService.GetSessionAsync(
                tenantId,
                sessionId);

        public Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTimeOffset at)
        {
            var authContext = AuthContext.System(tenantId, AuthOperation.Revoke, at);
            var command = new RevokeSessionCommand<TUserId>(tenantId,sessionId);

            return _orchestrator.ExecuteAsync(authContext, command);
        }

        public Task<ChainId?> ResolveChainIdAsync(string? tenantId, AuthSessionId sessionId)
            => _sessionQueryService.ResolveChainIdAsync(tenantId, sessionId);

        public Task RevokeAllChainsAsync(string? tenantId, TUserId userId, ChainId? exceptChainId, DateTimeOffset at)
        {
            var authContext = AuthContext.System(tenantId, AuthOperation.Revoke, at);
            var command = new RevokeAllChainsCommand<TUserId>(userId, exceptChainId);

            return _orchestrator.ExecuteAsync(authContext, command);
        }

        public Task RevokeChainAsync(string? tenantId, ChainId chainId, DateTimeOffset at)
        {
            var authContext = AuthContext.System(tenantId, AuthOperation.Revoke, at);
            var command = new RevokeChainCommand<TUserId>(chainId);

            return _orchestrator.ExecuteAsync(authContext, command);
        }

        public Task RevokeRootAsync(string? tenantId, TUserId userId, DateTimeOffset at)
        {
            var authContext = AuthContext.System(tenantId, AuthOperation.Revoke, at);
            var command = new RevokeRootCommand<TUserId>(userId);

            return _orchestrator.ExecuteAsync(authContext, command);
        }

        public async Task<ISession<TUserId>?> GetCurrentSessionAsync(string? tenantId, AuthSessionId sessionId)
        {
            var chainId = await _sessionQueryService.ResolveChainIdAsync(tenantId, sessionId);

            if (chainId is null)
                return null;

            var sessions = await _sessionQueryService.GetSessionsByChainAsync(tenantId, chainId.Value);

            return sessions.FirstOrDefault(s => s.SessionId == sessionId);
        }

        public Task<IssuedSession<TUserId>> IssueSessionAfterAuthenticationAsync(string? tenantId, AuthenticatedSessionContext<TUserId> context, CancellationToken ct = default)
        {
            var deviceContext = DeviceContext.From(context.DeviceInfo);
            var authContext = AuthContext.ForAuthenticatedUser(tenantId, AuthOperation.Login, context.Now, deviceContext);
            var command = new CreateLoginSessionCommand<TUserId>(context);

            return _orchestrator.ExecuteAsync(authContext, command, ct);
        }

    }
}
