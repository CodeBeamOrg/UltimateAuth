using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabApplicationPolicy(ILabRootResolver rootResolver) : IAccessPolicy
{
    public bool AppliesTo(AccessContext context)
        => context.Action.StartsWith(
            "authorization.roles.",
            StringComparison.Ordinal);

    public AccessDecision Decide(AccessContext context)
    {
        // Trusted internal operations.
        if (context.IsSystemActor)
            return AccessDecision.Allow();

        if (!context.IsAuthenticated ||
            context.ActorUserKey is null)
        {
            return AccessDecision.Deny("lab_authentication_required");
        }

        // Ladmin has unrestricted application-level access.
        if (rootResolver.IsLadmin(context))
            return AccessDecision.Allow();

        // Role reading is available to authenticated users.
        if (IsReadAction(context.Action))
            return AccessDecision.Allow();

        // TODO (v0.2.0):
        // Support sandbox-scoped role assignment and removal.
        //
        // Requirements:
        // - Owner may manage roles only for users in their sandbox.
        // - Ladmin, Owner and User are protected application roles.
        // - Owner cannot grant or revoke Ladmin privileges.
        // - Global role definitions and permissions remain protected.
        // - Validate the target user and target role using trusted
        //   operation metadata supplied by UltimateAuth.
        // - Enforce all checks through IAccessAuthority.

        return AccessDecision.Deny("lab_role_management_restricted");
    }

    private static bool IsReadAction(string action)
        => action is
            UAuthActions.Authorization.Roles.GetSelf or
            UAuthActions.Authorization.Roles.GetAdmin or
            UAuthActions.Authorization.Roles.QueryAdmin;
}
