using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class ExpiredSessionInvariant : IAuthorityInvariant
{
    public ValueTask<AccessDecisionResult> DecideAsync(AuthContext context, CancellationToken ct = default)
    {
        if (context.Operation == AuthOperation.Login)
            return ValueTask.FromResult(AccessDecisionResult.Allow());

        var session = context.Session;

        if (session is null)
            return ValueTask.FromResult(AccessDecisionResult.Allow());

        if (session.State == SessionState.Expired)
        {
            return ValueTask.FromResult(AccessDecisionResult.Deny("Session has expired."));
        }

        return ValueTask.FromResult(AccessDecisionResult.Allow());
    }
}
