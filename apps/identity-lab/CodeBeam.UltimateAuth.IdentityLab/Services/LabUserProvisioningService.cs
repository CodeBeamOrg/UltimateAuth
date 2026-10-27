using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.Users.Contracts;
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
    ILogger<LabUserProvisioningService> logger)
{
    public async Task<UserCreateResult> CreateMemberAsync(
        UserKey actorUserKey,
        CreateUserRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Obtain the actor from the actual authenticated session.
        var context = await contextProvider.CreateAsync(
            UAuthActions.Users.CreateAdmin,
            "users",
            ct);

        if (!context.IsAuthenticated ||
            context.IsSystemActor ||
            context.ActorUserKey is not UserKey authenticatedUserKey ||
            authenticatedUserKey != actorUserKey)
        {
            throw new UnauthorizedAccessException(
                "Authenticated actor mismatch.");
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
            throw new UnauthorizedAccessException(
                "Sandbox management permission required.");

        var remaining = await sandboxService.GetRemainingCapacityAsync(
            sandbox.Id,
            ct);

        if (remaining <= 0)
            throw new InvalidOperationException(
                "Sandbox user limit reached.");

        // Fail before account creation if the required role is absent.
        var roleStore = roleStoreFactory.Create(
            CodeBeam.UltimateAuth.Core.MultiTenancy.TenantKeys.Single);

        var role = await roleStore.GetByNameAsync("USER", ct);

        if (role is null || role.IsDeleted)
            throw new InvalidOperationException(
                "Identity Lab User role is not configured.");

        // Only this server-side path can attach the object-identity token.
        var trustedContext = context.WithAttribute(
            LabProvisioningGate.AttributeKey,
            LabProvisioningGate.Token);

        var result = await userApplicationService.CreateUserAsync(trustedContext, request, ct);

        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.UserKey))
        {
            return result;
        }

        var newUserKey = UserKey.FromString(result.UserKey);

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

            throw new LabPartialProvisioningException(
                newUserKey.Value,
                ex);
        }

        return result;
    }
}
