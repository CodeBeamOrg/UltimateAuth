using CodeBeam.UltimateAuth.Authorization.Policies;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Policies.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Policies;

internal sealed class ConditionalScopeBuilder : IPolicyScopeBuilder
{
    private readonly string _prefix;
    private readonly Func<AccessContext, bool> _condition;
    private readonly bool _expected;
    private readonly AccessPolicyRegistry _registry;
    private readonly IServiceProvider _services;

    public ConditionalScopeBuilder(string prefix, Func<AccessContext, bool> condition, bool expected, AccessPolicyRegistry registry, IServiceProvider services)
    {
        _prefix = prefix;
        _condition = condition;
        _expected = expected;
        _registry = registry;
        _services = services;
    }

    private IPolicyScopeBuilder Add<TPolicy>() where TPolicy : IAccessPolicy
    {
        _registry.Add(_prefix, sp => new ConditionalAccessPolicy(_condition, _expected, ActivatorUtilities.CreateInstance<TPolicy>(sp)));
        return this;
    }

    public IPolicyScopeBuilder RequireSelf() => Add<RequireSelfPolicy>();
    public IPolicyScopeBuilder RequirePermission() => Add<MustHavePermissionPolicy>();
    public IPolicyScopeBuilder RequireAuthenticated() => Add<RequireAuthenticatedPolicy>();
    public IPolicyScopeBuilder DenyCrossTenant() => Add<DenyCrossTenantPolicy>();
}
