using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Policies;

internal sealed class RequireAuthenticatedPolicy : IAccessPolicy
{
    public ValueTask<AccessDecision> DecideAsync(AccessContext context, CancellationToken ct = default)
    {
        return context.IsAuthenticated
            ? ValueTask.FromResult(AccessDecision.Allow())
            : ValueTask.FromResult(AccessDecision.Deny("unauthenticated"));
    }

    public bool AppliesTo(AccessContext context)
    {
        return !context.Action.EndsWith(".anonymous");
    }
}
