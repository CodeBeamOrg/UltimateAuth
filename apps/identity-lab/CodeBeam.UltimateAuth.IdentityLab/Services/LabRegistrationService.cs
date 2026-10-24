using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabRegistrationService(IUAuthClient uAuthClient, IRoleStoreFactory roleStoreFactory, IUserRoleStoreFactory userRoleStoreFactory, 
                    SandboxService sandboxService, ActivityLogService activityLogService)
{
    public async Task<LabRegistrationResult> RegisterAsync(LabRegistrationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return LabRegistrationResult.Failed("Username, email and password are required.");
        }

        var createResult = await uAuthClient.Users.CreateAsync(
            new CreateUserRequest
            {
                UserName = request.Username.Trim(),
                Email = request.Email.Trim(),
                Password = request.Password
            });

        if (!createResult.IsSuccess || createResult.Value is not
            {
                IsSuccess: true,
                UserKey: { Length: > 0 } createdUserKey
            })
        {
            return LabRegistrationResult.Failed(createResult.Value?.FailureReason ?? "Unable to create your account.");
        }

        var userKey = UserKey.FromString(createdUserKey);

        // TODO(Part 3): Replace direct role-store provisioning with
        // a trusted UltimateAuth registration/provisioning extension.
        // Handle partial failures and recovery atomically where possible.

        try
        {
            var tenant = TenantKeys.Single;

            var roleStore = roleStoreFactory.Create(tenant);
            var userRoleStore = userRoleStoreFactory.Create(tenant);

            var ownerRole = await roleStore.GetByNameAsync(LabRoles.Owner.ToUpperInvariant(), ct);

            if (ownerRole is null || ownerRole.IsDeleted)
            {
                return LabRegistrationResult.Failed("Owner role is not initialized.", requiresRepair: true);
            }

            var assignments = await userRoleStore.GetAssignmentsAsync(userKey, ct);

            if (!assignments.Any(x => x.RoleId == ownerRole.Id))
            {
                await userRoleStore.AssignAsync(userKey, ownerRole.Id, DateTimeOffset.UtcNow, ct);
            }

            // A sandbox name is presentation data, not an identity key.
            // The owner relationship is established using OwnerUserKey.
            var sandbox = await sandboxService.CreateAsync(userKey, $"{request.Username.Trim()}'s Sandbox", ct);

            await activityLogService.TryRecordAsync(sandbox.Id, userKey.Value, ActivityEvents.UserCreated, userKey.Value, ct);

            return LabRegistrationResult.Success();
        }
        catch (Exception)
        {
            // The UltimateAuth account may already exist.
            // Do not retry account creation automatically.

            // TODO(Part 3): Persist provisioning state or implement
            // authenticated repair for partially completed registration.

            return LabRegistrationResult.Failed("Your account was created, but sandbox setup could not be completed. Please contact support before registering again.", requiresRepair: true);
        }
    }
}
