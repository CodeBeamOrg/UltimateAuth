using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using System.Net;

namespace CodeBeam.UltimateAuth.Policies;

internal sealed class RequireAuthenticatedPolicy : IAccessPolicy
{
    public AccessDecision Decide(AccessContext context)
    {
        return context.IsAuthenticated
            ? AccessDecision.Allow()
            : AccessDecision.Deny("unauthenticated");
    }

    public bool AppliesTo(AccessContext context)
    {
        return !context.Action.EndsWith(".anonymous");
    }
}
