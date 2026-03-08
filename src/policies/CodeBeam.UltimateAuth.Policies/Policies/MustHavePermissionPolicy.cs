using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Constants;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Authorization.Policies;

public sealed class MustHavePermissionPolicy : IAccessPolicy
{
    public AccessDecision Decide(AccessContext context)
    {
        if (context.Attributes.TryGetValue(UAuthConstants.Access.Permissions, out var value) && value is CompiledPermissionSet permissions)
        {
            if (permissions.IsAllowed(context.Action))
                return AccessDecision.Allow();
        }

        return AccessDecision.Deny("missing_permission");
    }

    public bool AppliesTo(AccessContext context)
    {
        return context.Action.EndsWith(".admin", StringComparison.OrdinalIgnoreCase);
    }
}