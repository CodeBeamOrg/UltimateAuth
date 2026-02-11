using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class TenantResolvedInvariant : IAuthorityInvariant
{
    public AccessDecisionResult Decide(AuthContext context)
    {
        if (context.Tenant.IsUnresolved)
        {
            return AccessDecisionResult.Deny("Tenant is not resolved.");
        }

        return AccessDecisionResult.Allow();
    }
}
