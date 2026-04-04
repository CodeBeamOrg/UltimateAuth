using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Policies.Registry;

namespace CodeBeam.UltimateAuth.Policies.Defaults;

public sealed class CompiledAccessPolicySet
{
    private readonly PolicyRegistration[] _registrations;

    internal CompiledAccessPolicySet(PolicyRegistration[] registrations)
    {
        _registrations = registrations;
    }

    public IReadOnlyList<IAccessPolicy> Resolve(AccessContext context, IServiceProvider services)
    {
        var list = new List<IAccessPolicy>();

        foreach (var r in _registrations)
        {
            if (context.Action.StartsWith(r.ActionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var policy = r.Factory(services);

                if (policy.AppliesTo(context))
                    list.Add(policy);
            }
        }

        return list;
    }
}
