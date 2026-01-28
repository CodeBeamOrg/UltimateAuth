using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Policies;

internal sealed class RequireSelfPolicy : IAccessPolicy
{
    public AccessDecision Decide(AccessContext context)
    {
        if (!context.IsAuthenticated)
            return AccessDecision.Deny("unauthenticated");

        return context.IsSelfAction
            ? AccessDecision.Allow()
            : AccessDecision.Deny("not_self");
    }

    public bool AppliesTo(AccessContext context) => context.Action.EndsWith(".self");
}
