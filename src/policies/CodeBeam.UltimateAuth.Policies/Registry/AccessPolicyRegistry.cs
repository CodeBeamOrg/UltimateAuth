using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Policies.Defaults;

namespace CodeBeam.UltimateAuth.Policies.Registry;

public sealed class AccessPolicyRegistry
{
    private readonly List<PolicyRegistration> _policies = new();
    private bool _built;

    internal void Add(string actionPrefix, Func<IServiceProvider, IAccessPolicy> factory)
    {
        if (_built)
        {
            throw new InvalidOperationException("AccessPolicyRegistry is already built. Policies cannot be modified after Build().");
        }

        _policies.Add(new PolicyRegistration(actionPrefix, factory));
    }

    public IReadOnlyList<IAccessPolicy> Resolve(AccessContext context, IServiceProvider services)
    {
        return _policies
            .Where(p => context.Action.StartsWith(p.ActionPrefix, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Factory(services))
            .ToList();
    }

    public CompiledAccessPolicySet Build()
    {
        if (_built)
            throw new InvalidOperationException("AccessPolicyRegistry.Build() can only be called once.");

        _built = true;

        return new CompiledAccessPolicySet(_policies.OrderBy(r => r.ActionPrefix.Length).ToArray());
    }
}

internal sealed record PolicyRegistration(
    string ActionPrefix,
    Func<IServiceProvider, IAccessPolicy> Factory);
