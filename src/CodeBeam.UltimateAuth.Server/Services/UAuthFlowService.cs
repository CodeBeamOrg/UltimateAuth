using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Events;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Server.Services;

internal sealed class UAuthFlowService<TUserId> : IUAuthFlowService<TUserId>
{
    private readonly IAuthFlowContextAccessor _authFlow;
    private readonly IAuthFlowContextFactory _authFlowContextFactory;
    private readonly ILoginOrchestrator<TUserId> _loginOrchestrator;
    private readonly ISessionOrchestrator _orchestrator;
    private readonly UAuthEventDispatcher _events;

    public UAuthFlowService(
        IAuthFlowContextAccessor authFlow,
        IAuthFlowContextFactory authFlowContextFactory,
        ILoginOrchestrator<TUserId> loginOrchestrator,
        ISessionOrchestrator orchestrator,
        UAuthEventDispatcher events)
    {
        _authFlow = authFlow;
        _authFlowContextFactory = authFlowContextFactory;
        _loginOrchestrator = loginOrchestrator;
        _orchestrator = orchestrator;
        _events = events;
    }

    public Task<MfaChallengeResult> BeginMfaAsync(BeginMfaRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<LoginResult> CompleteMfaAsync(CompleteMfaRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<LoginResult> ExternalLoginAsync(ExternalLoginRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<LoginResult> LoginAsync(AuthFlowContext flow, LoginRequest request, CancellationToken ct = default)
    {
        return _loginOrchestrator.LoginAsync(flow, request, ct);
    }

    public async Task<LoginResult> LoginAsync(AuthFlowContext flow, AuthExecutionContext execution, LoginRequest request, CancellationToken ct = default)
    {
        var effectiveFlow = execution.EffectiveClientProfile is null
            ? flow
            : await _authFlowContextFactory.RecreateWithClientProfileAsync(flow, (UAuthClientProfile)execution.EffectiveClientProfile, ct);
        return await _loginOrchestrator.LoginAsync(effectiveFlow, request, ct);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
    {
        var authFlow = _authFlow.Current;
        var now = request.At ?? DateTimeOffset.UtcNow;
        var authContext = authFlow.ToAuthContext(now);

        var revoked = await _orchestrator.ExecuteAsync(authContext, new RevokeSessionCommand(request.SessionId), ct);

        if (!revoked)
            return;

        if (authFlow.UserKey is not UserKey uaKey)
            return;

        await _events.DispatchAsync(new UserLoggedOutContext(request.Tenant, uaKey, request.At ?? DateTimeOffset.Now, LogoutReason.Explicit, request.SessionId));
    }

    public async Task LogoutAllAsync(LogoutAllRequest request, CancellationToken ct = default)
    {
        var authFlow = _authFlow.Current;
        var now = request.At ?? DateTimeOffset.UtcNow;

        if (authFlow.Session is not SessionSecurityContext session)
            throw new InvalidOperationException("LogoutAll requires an active session.");

        var authContext = authFlow.ToAuthContext(now);
        SessionChainId? exceptChainId = null;

        if (request.ExceptCurrent)
        {
            exceptChainId = session.ChainId;

            if (exceptChainId is null)
                throw new InvalidOperationException("Current session chain could not be resolved.");
        }

        if (authFlow.UserKey is UserKey uaKey)
        {
            var command = new RevokeAllChainsCommand(uaKey, exceptChainId);
            await _orchestrator.ExecuteAsync(authContext, command, ct);
        }
    }

    public Task<ReauthResult> ReauthenticateAsync(ReauthRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
