using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Policies.Registry;

public sealed class PolicyRule
{
    public string ActionPrefix { get; }
    public Func<AccessContext, IAccessPolicy> Factory { get; }

    public PolicyRule(string actionPrefix, Func<AccessContext, IAccessPolicy> factory)
    {
        ActionPrefix = actionPrefix;
        Factory = factory;
    }

    public bool Matches(string action) => action.StartsWith(ActionPrefix, StringComparison.OrdinalIgnoreCase);
}
