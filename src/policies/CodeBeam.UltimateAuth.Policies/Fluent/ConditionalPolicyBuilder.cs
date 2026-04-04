using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Policies.Registry;

namespace CodeBeam.UltimateAuth.Policies;

internal sealed class ConditionalPolicyBuilder : IConditionalPolicyBuilder
{
    private readonly string _prefix;
    private readonly Func<AccessContext, bool> _condition;
    private readonly AccessPolicyRegistry _registry;
    private readonly IServiceProvider _services;

    public ConditionalPolicyBuilder(string prefix, Func<AccessContext, bool> condition, AccessPolicyRegistry registry, IServiceProvider services)
    {
        _prefix = prefix;
        _condition = condition;
        _registry = registry;
        _services = services;
    }

    public IPolicyScopeBuilder Then() => new ConditionalScopeBuilder(_prefix, _condition, true, _registry, _services);
    public IPolicyScopeBuilder Otherwise() => new ConditionalScopeBuilder(_prefix, _condition, false, _registry, _services);
}
