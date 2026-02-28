using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Server.Services;

internal sealed class UAuthSessionManager : IUAuthSessionManager
{
    private readonly IAuthFlowContextAccessor _authFlow;
    private readonly ISessionOrchestrator _orchestrator;
    private readonly IClock _clock;

    public UAuthSessionManager(IAuthFlowContextAccessor authFlow, ISessionOrchestrator orchestrator, IClock clock)
    {
        _authFlow = authFlow;
        _orchestrator = orchestrator;
        _clock = clock;
    }

    public Task RevokeSessionAsync(AuthSessionId sessionId, CancellationToken ct = default)
    {
        var authContext = _authFlow.Current.ToAuthContext(_clock.UtcNow);
        var command = new RevokeSessionCommand(sessionId);
        return _orchestrator.ExecuteAsync(authContext, command, ct);
    }

    public Task RevokeChainAsync(SessionChainId chainId, CancellationToken ct = default)
    {
        var authContext = _authFlow.Current.ToAuthContext(_clock.UtcNow);
        var command = new RevokeChainCommand(chainId);
        return _orchestrator.ExecuteAsync(authContext, command, ct);
    }

    public Task RevokeAllChainsAsync(UserKey userKey, SessionChainId? exceptChainId, CancellationToken ct = default)
    {
        var authContext = _authFlow.Current.ToAuthContext(_clock.UtcNow);
        var command = new RevokeAllChainsCommand(userKey, exceptChainId);
        return _orchestrator.ExecuteAsync(authContext, command, ct);
    }

    public Task RevokeRootAsync(UserKey userKey, CancellationToken ct = default)
    {
        var authContext = _authFlow.Current.ToAuthContext(_clock.UtcNow);
        var command = new RevokeRootCommand(userKey);
        return _orchestrator.ExecuteAsync(authContext, command, ct);
    }
}
