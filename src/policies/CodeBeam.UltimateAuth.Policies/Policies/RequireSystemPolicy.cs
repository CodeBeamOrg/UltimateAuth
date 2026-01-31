using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Policies
{
    internal sealed class RequireSystemPolicy : IAccessPolicy
    {
        public AccessDecision Decide(AccessContext context)
            => context.IsSystemActor
                ? AccessDecision.Allow()
                : AccessDecision.Deny("system_actor_required");

        public bool AppliesTo(AccessContext context) => context.Action.EndsWith(".system", StringComparison.Ordinal);
    }
}
