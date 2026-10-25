using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Policies;

internal sealed class DenyAdminSelfModificationPolicy : IAccessPolicy
{
    public ValueTask<AccessDecision> DecideAsync(AccessContext context, CancellationToken ct = default)
    {
        if (!context.IsAuthenticated)
            return ValueTask.FromResult(AccessDecision.Deny("unauthenticated"));

        if (context.ActorUserKey == context.TargetUserKey)
            return ValueTask.FromResult(AccessDecision.Deny("admin_cannot_modify_own_account"));

        return ValueTask.FromResult(AccessDecision.Allow());
    }

    public bool AppliesTo(AccessContext context)
    {
        if (!context.Action.EndsWith(".admin"))
            return false;

        if (context.TargetUserKey is null)
            return false;

        // READ actions allowed
        if (context.Action.Contains(".get") ||
            context.Action.Contains(".read") ||
            context.Action.Contains(".query"))
            return false;

        return true;
    }
}
