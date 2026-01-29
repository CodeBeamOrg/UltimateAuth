using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Abstractions;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class UserApplicationService : IUserApplicationService
{
    private readonly IAccessOrchestrator _accessOrchestrator;
    private readonly IUserLifecycleStore _lifecycleStore;
    private readonly IUserProfileStore _profileStore;
    private readonly IUserIdentifierStore _identifierStore;
    private readonly IEnumerable<IUserLifecycleIntegration> _integrations;
    private readonly IClock _clock;

    public UserApplicationService(
        IAccessOrchestrator accessOrchestrator,
        IUserLifecycleStore lifecycleStore,
        IUserProfileStore profileStore,
        IUserIdentifierStore identifierStore,
        IEnumerable<IUserLifecycleIntegration> integrations,
        IClock clock)
    {
        _accessOrchestrator = accessOrchestrator;
        _lifecycleStore = lifecycleStore;
        _profileStore = profileStore;
        _identifierStore = identifierStore;
        _integrations = integrations;
        _clock = clock;
    }

    public async Task<UserViewDto> GetMeAsync(AccessContext context, CancellationToken ct = default)
    {
        var command = new GetMeCommand(async innerCt =>
        {
            if (context.ActorUserKey is null)
                throw new UnauthorizedAccessException();

            return await BuildUserViewAsync(context.ResourceTenantId, context.ActorUserKey.Value, innerCt);
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task<UserViewDto> GetUserProfileAsync(AccessContext context, CancellationToken ct = default)
    {
        var command = new GetUserProfileCommand(async innerCt =>
        {
            // Target user MUST exist in context
            var targetUserKey = context.GetTargetUserKey();

            return await BuildUserViewAsync(context.ResourceTenantId, targetUserKey, innerCt);

        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task<UserCreateResult> CreateUserAsync(AccessContext context, CreateUserRequest request, CancellationToken ct = default)
    {
        var command = new CreateUserCommand(async innerCt =>
        {
            var now = _clock.UtcNow;
            var userKey = UserKey.New();

            if (!string.IsNullOrWhiteSpace(request.PrimaryIdentifierValue) && request.PrimaryIdentifierType is null)
            {
                return UserCreateResult.Failed("primary_identifier_type_required");
            }

            await _lifecycleStore.CreateAsync(context.ResourceTenantId,
                new UserLifecycle
                {
                    UserKey = userKey,
                    Status = UserStatus.Active,
                    CreatedAt = now
                },
                innerCt);

            await _profileStore.CreateAsync(context.ResourceTenantId,
                new UserProfile
                {
                    UserKey = userKey,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    DisplayName = request.DisplayName,
                    BirthDate = request.BirthDate,
                    Gender = request.Gender,
                    Bio = request.Bio,
                    Language = request.Language,
                    TimeZone = request.TimeZone,
                    Culture = request.Culture,
                    Metadata = request.Metadata,
                    CreatedAt = now
                },
                innerCt);

            if (!string.IsNullOrWhiteSpace(request.PrimaryIdentifierValue) && request.PrimaryIdentifierType is not null)
            {
                await _identifierStore.CreateAsync(context.ResourceTenantId,
                    new UserIdentifier
                    {
                        UserKey = userKey,
                        Type = request.PrimaryIdentifierType.Value,
                        Value = request.PrimaryIdentifierValue,
                        IsPrimary = true,
                        IsVerified = request.PrimaryIdentifierVerified,
                        CreatedAt = now,
                        VerifiedAt = request.PrimaryIdentifierVerified ? now : null
                    },
                    innerCt);
            }

            foreach (var integration in _integrations)
            {
                await integration.OnUserCreatedAsync(context.ResourceTenantId, userKey, request, innerCt);
            }

            return UserCreateResult.Success(userKey);
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task ChangeUserStatusAsync(AccessContext context, object request, CancellationToken ct = default)
    {
        var command = new ChangeUserStatusCommand(async innerCt =>
        {
            var newStatus = request switch
            {
                ChangeUserStatusSelfRequest r => r.NewStatus,
                ChangeUserStatusAdminRequest r => r.NewStatus,
                _ => throw new InvalidOperationException("invalid_request")
            };

            if (context.IsSelfAction)
            {
                if (newStatus is UserStatus.Disabled 
                    or UserStatus.Suspended
                    or UserStatus.Locked
                    or UserStatus.RiskHold
                    or UserStatus.PendingActivation
                    or UserStatus.PendingVerification)
                    throw new InvalidOperationException("self_cannot_set_admin_status");
            }

            if (!context.IsSelfAction)
            {
                if (newStatus is UserStatus.SelfSuspended or UserStatus.Deactivated)
                    throw new InvalidOperationException("admin_cannot_set_self_status");
            }

            var targetUserKey = context.GetTargetUserKey();
            await _lifecycleStore.ChangeStatusAsync(context.ResourceTenantId, targetUserKey, newStatus, _clock.UtcNow, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task UpdateUserProfileAsync(AccessContext context, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var command = new UpdateUserProfileCommand(async innerCt =>
        {
            var targetUserKey = context.GetTargetUserKey();
            var update = UserProfileMapper.ToUpdate(request);

            await _profileStore.UpdateAsync(context.ResourceTenantId, targetUserKey, update, _clock.UtcNow, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task AddUserIdentifierAsync(AccessContext context, AddUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new AddUserIdentifierCommand(async innerCt =>
        {
            var userKey = context.GetTargetUserKey();

            await _identifierStore.CreateAsync(context.ResourceTenantId,
                new UserIdentifier
                {
                    UserKey = userKey,
                    Type = request.Type,
                    Value = request.Value,
                    IsPrimary = request.IsPrimary,
                    IsVerified = false,
                    CreatedAt = _clock.UtcNow
                },
                innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task UpdateUserIdentifierAsync(AccessContext context, UpdateUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new UpdateUserIdentifierCommand(async innerCt =>
        {
            if (string.Equals(request.OldValue, request.NewValue, StringComparison.Ordinal))
                throw new InvalidOperationException("identifier_value_unchanged");

            await _identifierStore.UpdateValueAsync(context.ResourceTenantId, request.Type, request.OldValue, request.NewValue, _clock.UtcNow, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }


    public async Task SetPrimaryUserIdentifierAsync(AccessContext context, SetPrimaryUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new SetPrimaryUserIdentifierCommand(async innerCt =>
        {
            var userKey = context.GetTargetUserKey();

            await _identifierStore.SetPrimaryAsync(context.ResourceTenantId, userKey, request.Type, request.Value, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task VerifyUserIdentifierAsync(AccessContext context, VerifyUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new VerifyUserIdentifierCommand(async innerCt =>
        {
            await _identifierStore.MarkVerifiedAsync(context.ResourceTenantId, request.Type, request.Value, _clock.UtcNow, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task DeleteUserIdentifierAsync(AccessContext context, DeleteUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new DeleteUserIdentifierCommand(async innerCt =>
        {
            await _identifierStore.DeleteAsync(context.ResourceTenantId, request.Type, request.Value, request.Mode, _clock.UtcNow, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task DeleteUserAsync(AccessContext context, DeleteUserRequest request, CancellationToken ct = default)
    {
        var command = new DeleteUserCommand(async innerCt =>
        {
            var targetUserKey = context.GetTargetUserKey();
            var now = _clock.UtcNow;

            await _lifecycleStore.DeleteAsync(context.ResourceTenantId, targetUserKey, request.Mode, now, innerCt);
            await _identifierStore.DeleteByUserAsync(context.ResourceTenantId, targetUserKey, request.Mode, now, innerCt);
            await _profileStore.DeleteAsync(context.ResourceTenantId, targetUserKey, request.Mode, now, innerCt);

            foreach (var integration in _integrations)
            {
                await integration.OnUserDeletedAsync(context.ResourceTenantId, targetUserKey, request.Mode, innerCt);
            }
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    private async Task<UserViewDto> BuildUserViewAsync(string? tenantId, UserKey userKey, CancellationToken ct)
    {
        var profile = await _profileStore.GetAsync(tenantId, userKey, ct);

        if (profile is null || profile.IsDeleted)
            throw new InvalidOperationException("user_profile_not_found");

        var identifiers = await _identifierStore.GetByUserAsync(tenantId, userKey, ct);

        var username = identifiers.FirstOrDefault(x => x.Type == UserIdentifierType.Username && x.IsPrimary);
        var primaryEmail = identifiers.FirstOrDefault(x => x.Type == UserIdentifierType.Email && x.IsPrimary);
        var primaryPhone = identifiers.FirstOrDefault(x => x.Type == UserIdentifierType.Phone && x.IsPrimary);

        var dto = UserProfileMapper.ToDto(profile);

        return dto with
        {
            UserName = username?.Value,
            PrimaryEmail = primaryEmail?.Value,
            PrimaryPhone = primaryPhone?.Value,
            EmailVerified = primaryEmail?.IsVerified ?? false,
            PhoneVerified = primaryPhone?.IsVerified ?? false
        };
    }
}
