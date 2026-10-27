using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Persistence;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.IdentityLab.Policy;

public sealed class SandboxUAuthAccessPolicy(IDbContextFactory<LabDbContext> dbFactory, ILabRootResolver rootResolver) : IAccessPolicy
{
    public bool AppliesTo(AccessContext context)
        => IsProtectedAction(context.Action);

    public async ValueTask<AccessDecision> DecideAsync(AccessContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var action = context.Action;

        // Authentication recovery and registration require their own trusted, target-aware protection.
        if (action == UAuthActions.Users.CreateAnonymous)
        {
            return LabProvisioningGate.IsTrusted(context.Attributes)
                ? AccessDecision.Allow()
                : AccessDecision.Deny("lab_registration_required");
        }

        if (action.EndsWith(".anonymous", StringComparison.Ordinal))
            return AccessDecision.Allow();

        // Preserve logout capability.
        if (IsLogoutAction(action))
            return AccessDecision.Allow();

        if (context.IsSystemActor)
            return AccessDecision.Allow();

        if (action == UAuthActions.Users.CreateAdmin)
        {
            if (!LabProvisioningGate.IsTrusted(context.Attributes))
                return AccessDecision.Deny("sandbox_provisioning_required");
        }

        if (!context.IsAuthenticated || context.ActorUserKey is not UserKey actorKey)
        {
            return AccessDecision.Deny("sandbox_actor_required");
        }

        // Ladmin is the application root administrator.
        // Existing UltimateAuth policies still apply.
        if (rootResolver.IsLadmin(context))
            return AccessDecision.Allow();

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var actor = await db.SandboxMembers
            .AsNoTracking()
            .Where(x => x.UserKey == actorKey)
            .Select(x => new
            {
                x.SandboxId,
                x.Role,
                x.Sandbox.Status
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (actor is null)
            return AccessDecision.Deny("sandbox_membership_required");

        if (actor.Status == SandboxStatus.Suspended)
            return AccessDecision.Deny("sandbox_suspended");

        var operation = ClassifyAction(action);

        if (operation == SandboxOperation.Denied)
            return AccessDecision.Deny("sandbox_operation_restricted");

        if (actor.Status == SandboxStatus.Passive && operation != SandboxOperation.Read)
        {
            return AccessDecision.Deny("sandbox_read_only");
        }

        if (operation == SandboxOperation.Manage && actor.Role != SandboxMemberRole.Owner)
        {
            return AccessDecision.Deny("sandbox_owner_required");
        }

        if (action == UAuthActions.Users.GetByKeysAdmin)
        {
            var requestedKeys = context.TargetUserKeys;

            if (requestedKeys.Count == 0)
                return AccessDecision.Deny("sandbox_targets_required");

            var keys = requestedKeys.Distinct().ToArray();

            var allowedCount = await db.SandboxMembers
                .AsNoTracking()
                .CountAsync(x => x.SandboxId == actor.SandboxId && keys.Contains(x.UserKey), cancellationToken);

            if (allowedCount != keys.Length)
            {
                return AccessDecision.Deny("sandbox_target_out_of_scope");
            }
        }

        // All target-based admin operations must remain
        // inside the actor's sandbox.
        if (RequiresTarget(action))
        {
            if (context.TargetUserKey is not UserKey targetKey)
                return AccessDecision.Deny("sandbox_target_required");

            var belongsToSandbox = await db.SandboxMembers
                .AsNoTracking()
                .AnyAsync(x => x.UserKey == targetKey && x.SandboxId == actor.SandboxId, cancellationToken);

            if (!belongsToSandbox)
                return AccessDecision.Deny("sandbox_target_out_of_scope");
        }

        return AccessDecision.Allow();
    }

    private static bool IsProtectedAction(string action)
        => action.StartsWith("users.", StringComparison.Ordinal)
        || action.StartsWith("credentials.", StringComparison.Ordinal)
        || action.StartsWith("sessions.", StringComparison.Ordinal)
        || action.StartsWith("authorization.", StringComparison.Ordinal)
        || action.StartsWith("flows.", StringComparison.Ordinal);

    private static bool IsLogoutAction(string action)
        => action is
            UAuthActions.Flows.LogoutSelf or
            UAuthActions.Flows.LogoutDeviceSelf or
            UAuthActions.Flows.LogoutOthersSelf or
            UAuthActions.Flows.LogoutAllSelf;

    private static bool RequiresTarget(string action)
    {
        if (!action.EndsWith(".admin", StringComparison.Ordinal))
            return false;

        return action.StartsWith("users.profile.", StringComparison.Ordinal)
            || action.StartsWith("users.identifiers.", StringComparison.Ordinal)
            || action.StartsWith("users.status.", StringComparison.Ordinal)
            || action == UAuthActions.Users.DeleteAdmin
            || action.StartsWith("credentials.", StringComparison.Ordinal)
            || action.StartsWith("sessions.", StringComparison.Ordinal)
            || action is
                UAuthActions.Flows.LogoutDeviceAdmin or
                UAuthActions.Flows.LogoutOthersAdmin or
                UAuthActions.Flows.LogoutAllAdmin
            || action == UAuthActions.Authorization.Roles.GetAdmin;
    }

    private static SandboxOperation ClassifyAction(string action)
    {
        if (action == UAuthActions.Users.CreateAdmin)
            return SandboxOperation.Manage;

        if (action == UAuthActions.Users.QueryAdmin)
            return SandboxOperation.Denied;

        if (action.StartsWith("authorization.roles.", StringComparison.Ordinal))
        {
            return action is
                UAuthActions.Authorization.Roles.GetSelf or
                UAuthActions.Authorization.Roles.GetAdmin or
                UAuthActions.Authorization.Roles.QueryAdmin
                    ? SandboxOperation.Read
                    : SandboxOperation.Denied;
        }

        if (IsReadAction(action))
            return SandboxOperation.Read;

        if (IsAdminAction(action))
            return SandboxOperation.Manage;

        if (IsSelfWriteAction(action))
            return SandboxOperation.Write;

        return SandboxOperation.Denied;
    }

    private static bool IsReadAction(string action)
        => action is
            UAuthActions.Users.GetByKeysAdmin or
            UAuthActions.UserProfiles.GetSelf or
            UAuthActions.UserProfiles.GetAdmin or
            UAuthActions.UserIdentifiers.GetSelf or
            UAuthActions.UserIdentifiers.GetAdmin or
            UAuthActions.Credentials.ListSelf or
            UAuthActions.Credentials.ListAdmin or
            UAuthActions.Sessions.GetChainSelf or
            UAuthActions.Sessions.GetChainAdmin or
            UAuthActions.Sessions.ListChainsSelf or
            UAuthActions.Sessions.ListChainsAdmin or
            UAuthActions.Authorization.Permissions.GetSelf;

    private static bool IsAdminAction(string action)
    {
        // Only user-targeted administrative operations.
        if (!RequiresTarget(action))
            return false;

        return action.EndsWith(".admin", StringComparison.Ordinal);
    }

    private static bool IsSelfWriteAction(string action)
        => action is
            UAuthActions.Users.DeleteSelf or
            UAuthActions.Users.ChangeStatusSelf or
            UAuthActions.UserProfiles.CreateSelf or
            UAuthActions.UserProfiles.UpdateSelf or
            UAuthActions.UserProfiles.DeleteSelf or
            UAuthActions.UserIdentifiers.AddSelf or
            UAuthActions.UserIdentifiers.UpdateSelf or
            UAuthActions.UserIdentifiers.SetPrimarySelf or
            UAuthActions.UserIdentifiers.UnsetPrimarySelf or
            UAuthActions.UserIdentifiers.VerifySelf or
            UAuthActions.UserIdentifiers.DeleteSelf or
            UAuthActions.Credentials.AddSelf or
            UAuthActions.Credentials.ChangeSelf or
            UAuthActions.Credentials.RevokeSelf or
            UAuthActions.Credentials.ActivateSelf or
            UAuthActions.Sessions.RevokeChainSelf or
            UAuthActions.Sessions.RevokeAllChainsSelf or
            UAuthActions.Sessions.RevokeOtherChainsSelf;

    private enum SandboxOperation
    {
        Denied,
        Read,
        Write,
        Manage
    }
}
