using CodeBeam.UltimateAuth.Policies.Registry;

namespace CodeBeam.UltimateAuth.Policies
{
    internal sealed class PolicyBuilder : IPolicyBuilder
    {
        private readonly AccessPolicyRegistry _registry;
        private readonly IServiceProvider _services;

        public PolicyBuilder(AccessPolicyRegistry registry, IServiceProvider services)
        {
            _registry = registry;
            _services = services;
        }

        public IPolicyScopeBuilder For(string actionPrefix) => new PolicyScopeBuilder(actionPrefix, _registry, _services);

        public IPolicyScopeBuilder Global() => new PolicyScopeBuilder(string.Empty, _registry, _services);
    }
}
