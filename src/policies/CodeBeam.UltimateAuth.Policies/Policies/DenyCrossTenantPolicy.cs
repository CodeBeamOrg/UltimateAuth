using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Policies;

internal sealed class DenyCrossTenantPolicy : IAccessPolicy
{
    public ValueTask<AccessDecision> DecideAsync(AccessContext context, CancellationToken ct = default)
    {
        return context.IsCrossTenant
            ? ValueTask.FromResult(AccessDecision.Deny("cross_tenant_access_denied"))
            : ValueTask.FromResult(AccessDecision.Allow());
    }

    public bool AppliesTo(AccessContext context) => true;
}
