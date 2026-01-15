using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Infrastructure.Orchestrator;

// TODO: Add wrapper service in client project. Validate method also may add.
namespace CodeBeam.UltimateAuth.Server.Services
{
    internal sealed class UAuthSessionManager : IUAuthSessionManager
    {
        private readonly IAuthFlowContextAccessor _authFlow;
        private readonly ISessionOrchestrator _orchestrator;
        private readonly ISessionQueryService _sessionQueryService;
        private readonly IClock _clock;

        public UAuthSessionManager(IAuthFlowContextAccessor authFlow, ISessionOrchestrator orchestrator, ISessionQueryService sessionQueryService, IClock clock)
        {
            _authFlow = authFlow;
            _orchestrator = orchestrator;
            _sessionQueryService = sessionQueryService;
            _clock = clock;
        }

        public Task<IReadOnlyList<ISessionChain>> GetChainsAsync(
            string? tenantId,
            UserKey userKey)
            => _sessionQueryService.GetChainsByUserAsync(tenantId, userKey);

        public Task<IReadOnlyList<ISession>> GetSessionsAsync(
            string? tenantId,
            SessionChainId chainId)
            => _sessionQueryService.GetSessionsByChainAsync(tenantId, chainId);

        public Task<ISession?> GetSessionAsync(
            string? tenantId,
            AuthSessionId sessionId)
            => _sessionQueryService.GetSessionAsync(tenantId, sessionId);

        public Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTimeOffset at)
        {
            var authContext = _authFlow.Current.ToAuthContext(_clock.UtcNow);
            var command = new RevokeSessionCommand(tenantId, sessionId);

            return _orchestrator.ExecuteAsync(authContext, command);
        }

        public Task<SessionChainId?> ResolveChainIdAsync(string? tenantId, AuthSessionId sessionId)
            => _sessionQueryService.ResolveChainIdAsync(tenantId, sessionId);

        public Task RevokeAllChainsAsync(string? tenantId, UserKey userKey, SessionChainId? exceptChainId, DateTimeOffset at)
        {
            var authContext = _authFlow.Current.ToAuthContext(_clock.UtcNow);
            var command = new RevokeAllChainsCommand(userKey, exceptChainId);

            return _orchestrator.ExecuteAsync(authContext, command);
        }

        public Task RevokeChainAsync(string? tenantId, SessionChainId chainId, DateTimeOffset at)
        {
            var authContext = _authFlow.Current.ToAuthContext(_clock.UtcNow);
            var command = new RevokeChainCommand(chainId);

            return _orchestrator.ExecuteAsync(authContext, command);
        }

        public Task RevokeRootAsync(string? tenantId, UserKey userKey, DateTimeOffset at)
        {
            var authContext = _authFlow.Current.ToAuthContext(_clock.UtcNow);
            var command = new RevokeRootCommand(userKey);

            return _orchestrator.ExecuteAsync(authContext, command);
        }

        public async Task<ISession?> GetCurrentSessionAsync(string? tenantId, AuthSessionId sessionId)
        {
            var chainId = await _sessionQueryService.ResolveChainIdAsync(tenantId, sessionId);

            if (chainId is null)
                return null;

            var sessions = await _sessionQueryService.GetSessionsByChainAsync(tenantId, chainId.Value);

            return sessions.FirstOrDefault(s => s.SessionId == sessionId);
        }

    }
}
