using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Infrastructure.Orchestrator;

namespace CodeBeam.UltimateAuth.Server.Services
{
    internal sealed class DefaultSessionService : ISessionService
    {
        private readonly ISessionOrchestrator _orchestrator;
        private readonly IClock _clock;

        public DefaultSessionService(ISessionOrchestrator orchestrator, IClock clock)
        {
            _orchestrator = orchestrator;
            _clock = clock;
        }

        public Task RevokeAllAsync(AuthContext authContext, UserKey userKey, CancellationToken ct)
        {
            return _orchestrator.ExecuteAsync(authContext, new RevokeAllUserSessionsCommand(userKey), ct);
        }

        public Task RevokeAllExceptChainAsync(AuthContext authContext, UserKey userKey, SessionChainId exceptChainId, CancellationToken ct)
        {
            return _orchestrator.ExecuteAsync(authContext, new RevokeAllChainsCommand(userKey, exceptChainId), ct);
        }

        public Task RevokeRootAsync(AuthContext authContext, UserKey userKey, CancellationToken ct)
        {
            return _orchestrator.ExecuteAsync(authContext, new RevokeRootCommand(userKey), ct);
        }
    }
}
