using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class InvalidOrRevokedSessionInvariant : IAuthorityInvariant
{
    public ValueTask<AccessDecisionResult> DecideAsync(AuthContext context, CancellationToken ct = default)
    {
        if (context.Operation == AuthOperation.Login)
            return ValueTask.FromResult(AccessDecisionResult.Allow());

        var session = context.Session;

        if (session is null)
            return ValueTask.FromResult(AccessDecisionResult.Deny("Session is required for this operation."));

        if (session.State == SessionState.Invalid ||
            session.State == SessionState.NotFound ||
            session.State == SessionState.Revoked ||
            session.State == SessionState.SecurityMismatch ||
            session.State == SessionState.DeviceMismatch)
        {
            return ValueTask.FromResult(AccessDecisionResult.Deny($"Session state is invalid: {session.State}"));
        }

        return ValueTask.FromResult(AccessDecisionResult.Allow());
    }
}
