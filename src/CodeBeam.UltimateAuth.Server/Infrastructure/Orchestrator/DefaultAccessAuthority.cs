using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class DefaultAccessAuthority : IAccessAuthority
    {
        private readonly IEnumerable<IAccessInvariant> _invariants;
        private readonly IEnumerable<IAccessPolicy> _policies;

        public DefaultAccessAuthority(
            IEnumerable<IAccessInvariant> invariants,
            IEnumerable<IAccessPolicy> policies)
        {
            _invariants = invariants ?? Array.Empty<IAccessInvariant>();
            _policies = policies ?? Array.Empty<IAccessPolicy>();
        }

        public AccessDecision Decide(AccessContext context)
        {
            foreach (var invariant in _invariants)
            {
                var result = invariant.Decide(context);
                if (!result.IsAllowed)
                    return result;
            }

            bool requiresReauth = false;

            foreach (var policy in _policies)
            {
                if (!policy.AppliesTo(context))
                    continue;

                var result = policy.Decide(context);

                if (!result.IsAllowed)
                    return result;

                if (result.RequiresReauthentication)
                    requiresReauth = true;
            }

            return requiresReauth
                ? AccessDecision.ReauthenticationRequired()
                : AccessDecision.Allow();
        }
    }

}
