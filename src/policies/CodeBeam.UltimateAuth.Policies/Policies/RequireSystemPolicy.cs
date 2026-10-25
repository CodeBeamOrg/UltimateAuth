using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Policies;

internal sealed class RequireSystemPolicy : IAccessPolicy
{
    public ValueTask<AccessDecision> DecideAsync(AccessContext context, CancellationToken ct = default)
        => context.IsSystemActor
            ? ValueTask.FromResult(AccessDecision.Allow())
            : ValueTask.FromResult(AccessDecision.Deny("system_actor_required"));

    public bool AppliesTo(AccessContext context) => context.Action.EndsWith(".system", StringComparison.Ordinal);
}
