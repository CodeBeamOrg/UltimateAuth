using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Policies;

internal sealed class RequireSelfOrAdminPolicy : IAccessPolicy
{
    public AccessDecision Decide(AccessContext context)
    {
        if (!context.IsAuthenticated)
            return AccessDecision.Deny("unauthenticated");

        if (context.IsSelfAction)
            return AccessDecision.Allow();

        if (context.Attributes.TryGetValue("roles", out var value)
            && value is IReadOnlyCollection<string> roles
            && roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            return AccessDecision.Allow();
        }

        return AccessDecision.Deny("self_or_admin_required");
    }

    public bool AppliesTo(AccessContext context)
    {
        if (context.Action.EndsWith(".anonymous"))
        {
           return false;
        }

        return !context.Action.EndsWith(".self") && !context.Action.EndsWith(".admin");
    }
}
