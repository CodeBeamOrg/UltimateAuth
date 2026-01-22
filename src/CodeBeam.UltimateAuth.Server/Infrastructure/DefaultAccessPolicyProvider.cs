using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Policies.Abstractions;
using CodeBeam.UltimateAuth.Policies.Defaults;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class DefaultAccessPolicyProvider : IAccessPolicyProvider
{
    private readonly CompiledAccessPolicySet _set;
    private readonly IServiceProvider _services;

    public DefaultAccessPolicyProvider(CompiledAccessPolicySet set, IServiceProvider services)
    {
        _set = set;
        _services = services;
    }

    public IReadOnlyCollection<IAccessPolicy> GetPolicies(AccessContext context) => _set.Resolve(context, _services);

}
