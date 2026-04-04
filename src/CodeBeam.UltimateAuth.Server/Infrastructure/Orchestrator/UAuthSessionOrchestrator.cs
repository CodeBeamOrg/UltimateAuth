using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class UAuthSessionOrchestrator : ISessionOrchestrator
{
    private readonly IAuthAuthority _authority;
    private readonly ISessionIssuer _issuer;
    private bool _executed;

    public UAuthSessionOrchestrator(IAuthAuthority authority, ISessionIssuer issuer)
    {
        _authority = authority;
        _issuer = issuer;
    }

    public async Task<TResult> ExecuteAsync<TResult>(AuthContext authContext, ISessionCommand<TResult> command, CancellationToken ct = default)
    {
        if (_executed)
            throw new InvalidOperationException("Session orchestrator can only be executed once per operation.");

        _executed = true;

        var decision = _authority.Decide(authContext);

        switch (decision.Decision)
        {
            case AuthorizationDecision.Deny:
                throw new UAuthAuthenticationException(decision.Reason ?? "authorization_denied");

            case AuthorizationDecision.Challenge:
                throw new UAuthChallengeRequiredException(decision.Reason);

            case AuthorizationDecision.Allow:
                break;
        }

        return await command.ExecuteAsync(authContext, _issuer, ct);
    }
}
