using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Policies;

internal sealed class RequireSelfPolicy : IAccessPolicy
{
    public ValueTask<AccessDecision> DecideAsync(AccessContext context, CancellationToken ct = default)
    {
        if (!context.IsAuthenticated)
            return ValueTask.FromResult(AccessDecision.Deny("unauthenticated"));

        return context.IsSelfAction
            ? ValueTask.FromResult(AccessDecision.Allow())
            : ValueTask.FromResult(AccessDecision.Deny("not_self"));
    }

    public bool AppliesTo(AccessContext context)
    {
        return context.Action.EndsWith(".self");
    }
}
