using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Policies;

internal sealed class DenyAdminSelfModificationPolicy : IAccessPolicy
{
    public AccessDecision Decide(AccessContext context)
    {
        if (!context.IsAuthenticated)
            return AccessDecision.Deny("unauthenticated");

        if (context.ActorUserKey == context.TargetUserKey)
            return AccessDecision.Deny("admin_cannot_modify_own_account");

        return AccessDecision.Allow();
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
