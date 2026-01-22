using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Policies.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Policies
{
    internal sealed class PolicyScopeBuilder : IPolicyScopeBuilder
    {
        private readonly string _prefix;
        private readonly AccessPolicyRegistry _registry;
        private readonly IServiceProvider _services;

        public PolicyScopeBuilder(
            string prefix,
            AccessPolicyRegistry registry,
            IServiceProvider services)
        {
            _prefix = prefix;
            _registry = registry;
            _services = services;
        }

        public IPolicyScopeBuilder RequireAuthenticated()
            => Add<RequireAuthenticatedPolicy>();

        public IPolicyScopeBuilder RequireSelf()
            => Add<RequireSelfPolicy>();

        public IPolicyScopeBuilder RequireAdmin()
            => Add<RequireAdminPolicy>();

        public IPolicyScopeBuilder RequireSelfOrAdmin()
            => Add<RequireSelfOrAdminPolicy>();

        public IPolicyScopeBuilder DenyCrossTenant()
            => Add<DenyCrossTenantPolicy>();

        private IPolicyScopeBuilder Add<TPolicy>()
            where TPolicy : IAccessPolicy
        {
            _registry.Add(_prefix, sp => ActivatorUtilities.CreateInstance<TPolicy>(sp));
            return this;
        }

        public IConditionalPolicyBuilder When(Func<AccessContext, bool> predicate)
        {
            return new ConditionalPolicyBuilder(_prefix, predicate, _registry, _services);
        }

    }

}
