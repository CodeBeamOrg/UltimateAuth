using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Policies.Abstractions;

namespace CodeBeam.UltimateAuth.Server.ResourceApi
{
    internal sealed class AllowAllAccessPolicyProvider : IAccessPolicyProvider
    {
        public IReadOnlyCollection<IAccessPolicy> GetPolicies(AccessContext context)
        {
            throw new NotSupportedException();
        }

        public Task<IAccessPolicy?> ResolveAsync(string name, CancellationToken ct = default)
            => Task.FromResult<IAccessPolicy?>(new AllowAllPolicy());
    }

    internal sealed class AllowAllPolicy : IAccessPolicy
    {
        public bool AppliesTo(AccessContext context)
        {
            throw new NotImplementedException();
        }

        public AccessDecision Decide(AccessContext context)
        {
            throw new NotImplementedException();
        }

        public Task<bool> EvaluateAsync(AccessContext context, CancellationToken ct = default)
            => Task.FromResult(true);
    }
}
