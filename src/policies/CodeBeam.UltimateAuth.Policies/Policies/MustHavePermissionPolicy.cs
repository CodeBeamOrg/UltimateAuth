using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;

namespace CodeBeam.UltimateAuth.Authorization.Policies;

public sealed class MustHavePermissionPolicy : IAccessPolicy
{
    public ValueTask<AccessDecision> DecideAsync(AccessContext context, CancellationToken ct = default)
    {
        if (context.Attributes.TryGetValue(UAuthConstants.Access.Permissions, out var value) && value is CompiledPermissionSet permissions)
        {
            if (permissions.IsAllowed(context.Action))
                return ValueTask.FromResult(AccessDecision.Allow());
        }

        return ValueTask.FromResult(AccessDecision.Deny("missing_permission"));
    }

    public bool AppliesTo(AccessContext context)
    {
        return context.Action.EndsWith(".admin", StringComparison.OrdinalIgnoreCase);
    }
}
