using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public sealed class LabRegistrationService(LabAccessContextProvider contextProvider, IUserApplicationService userApplicationService, IRoleStoreFactory roleStoreFactory,
                                           IUserRoleStoreFactory userRoleStoreFactory, SandboxService sandboxService, ActivityLogService activityLogService)
{
    public async Task<LabRegistrationResult> RegisterAsync(LabRegistrationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return LabRegistrationResult.Failed("Username, email and password are required.");
        }

        var context = await contextProvider.CreateAsync(UAuthActions.Users.CreateAnonymous, "users", ct);

        var trustedContext = context.WithAttribute(LabProvisioningGate.AttributeKey, LabProvisioningGate.Token);


        var createResult = await userApplicationService.CreateUserAsync(trustedContext,
            new CreateUserRequest
            {
                UserName = request.Username.Trim(),
                Email = request.Email.Trim(),
                Password = request.Password,
                FirstName = string.IsNullOrWhiteSpace(request.FirstName)
                    ? null
                    : request.FirstName.Trim(),
                LastName = string.IsNullOrWhiteSpace(request.LastName)
                    ? null
                    : request.LastName.Trim(),
                BirthDate = request.BirthDate
            },
            ct);

        if (!createResult.IsSuccess ||
            string.IsNullOrWhiteSpace(createResult.UserKey))
        {
            return LabRegistrationResult.Failed(createResult.FailureReason ?? "Unable to create your account.");
        }

        var userKey = UserKey.FromString(createResult.UserKey);

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

    public async Task<UserIdentifierAvailabilityResult> CheckAvailabilityAsync(UserIdentifierType type, string value, CancellationToken ct = default)
    {
        var context = await contextProvider.CreateAsync(UAuthActions.UserIdentifiers.CheckAvailability, "users", ct);

        return await userApplicationService.CheckIdentifierAvailabilityAsync(context,
            new CheckUserIdentifierAvailabilityRequest
            {
                Type = type,
                Value = value
            },
            ct);
    }
}
