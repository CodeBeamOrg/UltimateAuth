using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class ExpiredSessionInvariant : IAuthorityInvariant
{
    public AccessDecisionResult Decide(AuthContext context)
    {
        if (context.Operation == AuthOperation.Login)
            return AccessDecisionResult.Allow();

        var session = context.Session;

        if (session is null)
            return AccessDecisionResult.Allow();

        if (session.State == SessionState.Expired)
        {
            return AccessDecisionResult.Deny("Session has expired.");
        }

        return AccessDecisionResult.Allow();
    }
}
