using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Infrastructure
{
    public sealed class DefaultAuthAuthority : IAuthAuthority
    {
        private readonly IEnumerable<IAuthorityInvariant> _invariants;
        private readonly IEnumerable<IAuthorityPolicy> _policies;

        public AuthorizationResult Decide(AuthContext context)
        {
            // 1. Invariants
            foreach (var invariant in _invariants)
            {
                var result = invariant.Decide(context);
                if (!result.IsAllowed)
                    return result;
            }

            // 2. Policies
            bool challenged = false;

            foreach (var policy in _policies)
            {
                if (!policy.AppliesTo(context))
                    continue;

                var result = policy.Decide(context);

                if (!result.IsAllowed)
                    return result;

                if (result.RequiresChallenge)
                    challenged = true;
            }

            return challenged
                ? AuthorizationResult.Challenge("Additional verification required.")
                : AuthorizationResult.Allow();
        }
    }
}
