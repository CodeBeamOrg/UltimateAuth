using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Authorization.Policies;

public sealed class MustHavePermissionPolicy : IAccessPolicy
{
    public AccessDecision Decide(AccessContext context)
    {
        if (context.Attributes.TryGetValue("permissions", out var value) && value is IReadOnlyCollection<Permission> permissions)
        {
            var actionPermission = Permission.From(context.Action);

            if (permissions.Contains(actionPermission) ||
                permissions.Contains(Permission.Wildcard))
            {
                return AccessDecision.Allow();
            }
        }

        return AccessDecision.Deny("missing_permission");
    }

    public bool AppliesTo(AccessContext context)
    {
        return context.Action.EndsWith(".admin");
    }
}