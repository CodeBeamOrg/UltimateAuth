using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Policies.Abstractions;

public interface IAccessPolicyProvider
{
    IReadOnlyCollection<IAccessPolicy> GetPolicies(AccessContext context);
}
