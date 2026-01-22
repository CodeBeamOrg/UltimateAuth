using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Policies;

internal sealed class RequireAdminPolicy : IAccessPolicy
{
    public AccessDecision Decide(AccessContext context)
    {
        if (!context.IsAuthenticated)
            return AccessDecision.Deny("unauthenticated");

        if (!context.Attributes.TryGetValue("roles", out var value))
            return AccessDecision.Deny("missing_roles");

        if (value is not IReadOnlyCollection<string> roles)
            return AccessDecision.Deny("invalid_roles");

        return roles.Contains("Admin", StringComparer.OrdinalIgnoreCase)
            ? AccessDecision.Allow()
            : AccessDecision.Deny("admin_required");
    }

    public bool AppliesTo(AccessContext context) => true;
}
