using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Policies;

internal sealed class DenyCrossTenantPolicy : IAccessPolicy
{
    public AccessDecision Decide(AccessContext context)
    {
        return context.IsCrossTenant
            ? AccessDecision.Deny("cross_tenant_access_denied")
            : AccessDecision.Allow();
    }

    public bool AppliesTo(AccessContext context) => true;
}
