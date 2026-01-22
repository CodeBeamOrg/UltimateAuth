using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Policies;

internal sealed class RequireAuthenticatedPolicy : IAccessPolicy
{
    public AccessDecision Decide(AccessContext context)
    {
        return context.IsAuthenticated
            ? AccessDecision.Allow()
            : AccessDecision.Deny("unauthenticated");
    }

    public bool AppliesTo(AccessContext context) => true;
}
