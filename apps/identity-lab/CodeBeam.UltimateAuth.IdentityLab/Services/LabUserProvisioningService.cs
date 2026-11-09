using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.AspNetCore.Identity;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabUserProvisioningService(
    LabAccessContextProvider contextProvider,
    SandboxService sandboxService,
    SandboxAccessService accessService,
    IUserApplicationService userApplicationService,
    IRoleStoreFactory roleStoreFactory,
    IUserRoleStoreFactory userRoleStoreFactory,
    LabProvisioningRollbackGate rollbackGate,
    ILogger<LabUserProvisioningService> logger)
{
    public async Task<UserCreateResult> CreateMemberAsync(AccessContext context, AuthFlowContext flow, CreateUserRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!flow.IsAuthenticated || flow.UserKey != context.ActorUserKey || flow.Tenant != context.ActorTenant ||
            !context.IsAuthenticated ||
            context.IsSystemActor ||
            context.ActorUserKey is not UserKey authenticatedUserKey ||
            context.ActorTenant != TenantKeys.Single ||
            context.IsCrossTenant || context.Action != UAuthActions.Users.CreateAdmin)
        {
            throw new UnauthorizedAccessException("Authenticated actor mismatch.");
        }

        var sandbox = await sandboxService.GetByUserAsync(
            authenticatedUserKey, ct);

        if (sandbox is null)
            throw new UnauthorizedAccessException("Sandbox not found.");

        var access = await accessService.CanManageAsync(
            authenticatedUserKey,
            sandbox.Id,
            ct);

        if (!access.Allowed)
            throw new UnauthorizedAccessException("Sandbox management permission required.");

        var remaining = await sandboxService.GetRemainingCapacityAsync(sandbox.Id, ct);

        if (remaining <= 0)
            throw new InvalidOperationException("Sandbox user limit reached.");

        // Fail before account creation if the required role is absent.
        var roleStore = roleStoreFactory.Create(
            TenantKeys.Single);

        var role = await roleStore.GetByNameAsync("USER", ct);

        if (role is null || role.IsDeleted)
            throw new InvalidOperationException(
                "Identity Lab User role is not configured.");

        // Only this server-side path can attach the object-identity token.
        var trustedContext = context.WithAttribute(LabProvisioningGate.AttributeKey, LabProvisioningGate.Token);

        var result = await userApplicationService.CreateUserAsync(trustedContext, request, ct);

        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.UserKey))
        {
            return result;
        }


        var newUserKey = UserKey.FromString(result.UserKey);

        var rollbackToken = rollbackGate.Register(
            authenticatedUserKey,
            newUserKey);

        try
        {
            var userRoleStore = userRoleStoreFactory.Create(
                trustedContext.ResourceTenant);

            var assignments = await userRoleStore.GetAssignmentsAsync(
                newUserKey, ct);

            if (!assignments.Any(x => x.RoleId == role.Id))
            {
                await userRoleStore.AssignAsync(
                    newUserKey,
                    role.Id,
                    DateTimeOffset.UtcNow,
                    ct);
            }

            await sandboxService.AddMemberAsync(
                authenticatedUserKey,
                newUserKey,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Partial provisioning failure for user {UserKey}",
                newUserKey.Value);

            try
            {
                // Do not reuse the cancelled provisioning token.
                using var cleanupCts =
                    new CancellationTokenSource(TimeSpan.FromSeconds(15));

                var deleteContext = await contextProvider.CreateForValidatedUserAsync(
                    flow, context,
                    UAuthActions.Users.DeleteAdmin,
                    "users",
                    newUserKey,
                    cleanupCts.Token);

                if (deleteContext.ActorUserKey != authenticatedUserKey ||
                    !deleteContext.IsAuthenticated ||
                    deleteContext.IsSystemActor)
                {
                    throw new UnauthorizedAccessException(
                        "Rollback actor mismatch.");
                }

                var trustedDeleteContext = deleteContext.WithAttribute(
                    LabProvisioningRollbackGate.AttributeKey,
                    rollbackToken);

                await userApplicationService.DeleteUserAsync(
                    trustedDeleteContext,
                    new DeleteUserRequest
                    {
                        Mode = DeleteMode.Hard
                    },
                    cleanupCts.Token);

                logger.LogInformation(
                    "Provisioning rollback completed for {UserKey}",
                    newUserKey.Value);
            }
            catch (Exception rollbackEx)
            {
                logger.LogCritical(
                    rollbackEx,
                    "Provisioning rollback failed for {UserKey}. " +
                    "Manual reconciliation may be required.",
                    newUserKey.Value);
            }

            throw new LabPartialProvisioningException(
                newUserKey.Value,
                ex);
        }
        finally
        {
            rollbackGate.Revoke(rollbackToken);
        }

        return result;
    }
}
