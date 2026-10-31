using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabApplicationPolicy() : IAccessPolicy
{
    public bool AppliesTo(AccessContext context)
        => context.Action.StartsWith("authorization.roles.", StringComparison.Ordinal);

    public ValueTask<AccessDecision> DecideAsync(AccessContext context, CancellationToken cancellationToken = default)
    {
        // Trusted framework/system operations only.
        if (context.IsSystemActor)
            return ValueTask.FromResult(AccessDecision.Allow());

        if (!context.IsAuthenticated || context.ActorUserKey is null)
        {
            return ValueTask.FromResult(AccessDecision.Deny("lab_authentication_required"));
        }

        // Public role mutation is disabled for every user,
        // including Ladmin.
        //
        // Identity Lab system roles are managed through trusted
        // provisioning. Demo roles are managed through the
        // sandbox-scoped LabDemoRoleService.
        if (!IsReadAction(context.Action))
        {
            return ValueTask.FromResult(AccessDecision.Deny("lab_role_management_restricted"));
        }

        // Preserve existing role-reading behavior.
        return ValueTask.FromResult(AccessDecision.Allow());
    }

    private static bool IsReadAction(string action)
        => action is
            UAuthActions.Authorization.Roles.GetSelf or
            UAuthActions.Authorization.Roles.GetAdmin or
            UAuthActions.Authorization.Roles.QueryAdmin;
}
