using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class TenantResolvedInvariant : IAuthorityInvariant
{
    public ValueTask<AccessDecisionResult> DecideAsync(AuthContext context, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var result = context.Tenant.IsUnresolved
            ? AccessDecisionResult.Deny("Tenant is not resolved.")
            : AccessDecisionResult.Allow();

        return ValueTask.FromResult(result);
    }
}
