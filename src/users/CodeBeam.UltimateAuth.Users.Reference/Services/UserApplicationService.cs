using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Abstractions;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference.Commands;

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

            return await BuildUserViewAsync(context.ResourceTenant, context.ActorUserKey.Value, innerCt);
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task<UserViewDto> GetUserProfileAsync(AccessContext context, CancellationToken ct = default)
    {
        var command = new GetUserProfileCommand(async innerCt =>
        {
            // Target user MUST exist in context
            var targetUserKey = context.GetTargetUserKey();

            return await BuildUserViewAsync(context.ResourceTenant, targetUserKey, innerCt);

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

            await _lifecycleStore.CreateAsync(context.ResourceTenant,
                new UserLifecycle
                {
                    UserKey = userKey,
                    Status = UserStatus.Active,
                    CreatedAt = now
                },
                innerCt);

            await _profileStore.CreateAsync(context.ResourceTenant,
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
                await _identifierStore.CreateAsync(context.ResourceTenant,
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
                await integration.OnUserCreatedAsync(context.ResourceTenant, userKey, request, innerCt);
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

            var targetUserKey = context.GetTargetUserKey();
            var current = await _lifecycleStore.GetAsync(context.ResourceTenant, targetUserKey, innerCt);

            if (current is null)
                throw new InvalidOperationException("user_not_found");

            if (context.IsSelfAction && !IsSelfTransitionAllowed(current.Status, newStatus))
                throw new InvalidOperationException("self_transition_not_allowed");

            if (!context.IsSelfAction)
            {
                if (newStatus is UserStatus.SelfSuspended or UserStatus.Deactivated)
                    throw new InvalidOperationException("admin_cannot_set_self_status");
            }
            
            await _lifecycleStore.ChangeStatusAsync(context.ResourceTenant, targetUserKey, newStatus, _clock.UtcNow, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task UpdateUserProfileAsync(AccessContext context, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var command = new UpdateUserProfileCommand(async innerCt =>
        {
            var targetUserKey = context.GetTargetUserKey();
            var update = UserProfileMapper.ToUpdate(request);

            await _profileStore.UpdateAsync(context.ResourceTenant, targetUserKey, update, _clock.UtcNow, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task<IReadOnlyList<UserIdentifierDto>> GetIdentifiersByUserAsync(AccessContext context, CancellationToken ct = default)
    {
        var command = new GetUserIdentifiersCommand(async innerCt =>
        {
            var targetUserKey = context.GetTargetUserKey();
            var identifiers = await _identifierStore.GetByUserAsync(context.ResourceTenant, targetUserKey, innerCt);

            return identifiers.Select(UserIdentifierMapper.ToDto).ToList().AsReadOnly();
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task<UserIdentifierDto?> GetIdentifierAsync(AccessContext context, UserIdentifierType type, string value, CancellationToken ct = default)
    {
        var command = new GetUserIdentifierCommand(async innerCt =>
        {
            var identifier = await _identifierStore.GetAsync(context.ResourceTenant, type, value, innerCt);
            return identifier is null
                ? null
                : UserIdentifierMapper.ToDto(identifier);
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task<bool> UserIdentifierExistsAsync(AccessContext context, UserIdentifierType type, string value, CancellationToken ct = default)
    {
        var command = new UserIdentifierExistsCommand(async innerCt =>
        {
            return await _identifierStore.ExistsAsync(context.ResourceTenant, type, value, innerCt);
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task AddUserIdentifierAsync(AccessContext context, AddUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new AddUserIdentifierCommand(async innerCt =>
        {
            var userKey = context.GetTargetUserKey();

            await _identifierStore.CreateAsync(context.ResourceTenant,
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

            await _identifierStore.UpdateValueAsync(context.ResourceTenant, request.Type, request.OldValue, request.NewValue, _clock.UtcNow, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task SetPrimaryUserIdentifierAsync(AccessContext context, SetPrimaryUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new SetPrimaryUserIdentifierCommand(async innerCt =>
        {
            var userKey = context.GetTargetUserKey();

            await _identifierStore.SetPrimaryAsync(context.ResourceTenant, userKey, request.Type, request.Value, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task UnsetPrimaryUserIdentifierAsync(AccessContext context, UnsetPrimaryUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new UnsetPrimaryUserIdentifierCommand(async innerCt =>
        {
            var userKey = context.GetTargetUserKey();

            var identifiers = await _identifierStore.GetByUserAsync(context.ResourceTenant, userKey, innerCt);
            var target = identifiers.FirstOrDefault(i => i.Type == request.Type && string.Equals(i.Value, request.Value, StringComparison.OrdinalIgnoreCase) && !i.IsDeleted);

            if (target is null)
                throw new InvalidOperationException("identifier_not_found");

            if (!target.IsPrimary)
                throw new InvalidOperationException("identifier_not_primary");

            var otherLoginIdentifiers = identifiers.Where(i => !i.IsDeleted && IsLoginIdentifier(i.Type) && !(i.Type == target.Type && i.Value == target.Value)).ToList();
            if (otherLoginIdentifiers.Count == 0)
                throw new InvalidOperationException("cannot_unset_last_primary_login_identifier");

            await _identifierStore.UnsetPrimaryAsync(context.ResourceTenant, userKey, target.Type, target.Value, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task VerifyUserIdentifierAsync(AccessContext context, VerifyUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new VerifyUserIdentifierCommand(async innerCt =>
        {
            await _identifierStore.MarkVerifiedAsync(context.ResourceTenant, request.Type, request.Value, _clock.UtcNow, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task DeleteUserIdentifierAsync(AccessContext context, DeleteUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new DeleteUserIdentifierCommand(async innerCt =>
        {
            var targetUserKey = context.GetTargetUserKey();

            var identifiers = await _identifierStore.GetByUserAsync(context.ResourceTenant, targetUserKey, innerCt);
            var target = identifiers.FirstOrDefault(i => i.Type == request.Type && string.Equals(i.Value, request.Value, StringComparison.OrdinalIgnoreCase) && !i.IsDeleted);

            if (target is null)
                throw new InvalidOperationException("identifier_not_found");

            var loginIdentifiers = identifiers.Where(i => !i.IsDeleted && IsLoginIdentifier(i.Type)).ToList();
            if (IsLoginIdentifier(target.Type) && loginIdentifiers.Count == 1)
                throw new InvalidOperationException("cannot_delete_last_login_identifier");

            if (target.IsPrimary)
                throw new InvalidOperationException("cannot_delete_primary_identifier");

            await _identifierStore.DeleteAsync(context.ResourceTenant, request.Type, request.Value, request.Mode, _clock.UtcNow, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task DeleteUserAsync(AccessContext context, DeleteUserRequest request, CancellationToken ct = default)
    {
        var command = new DeleteUserCommand(async innerCt =>
        {
            var targetUserKey = context.GetTargetUserKey();
            var now = _clock.UtcNow;

            await _lifecycleStore.DeleteAsync(context.ResourceTenant, targetUserKey, request.Mode, now, innerCt);
            await _identifierStore.DeleteByUserAsync(context.ResourceTenant, targetUserKey, request.Mode, now, innerCt);
            await _profileStore.DeleteAsync(context.ResourceTenant, targetUserKey, request.Mode, now, innerCt);

            foreach (var integration in _integrations)
            {
                await integration.OnUserDeletedAsync(context.ResourceTenant, targetUserKey, request.Mode, innerCt);
            }
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    private async Task<UserViewDto> BuildUserViewAsync(TenantKey tenant, UserKey userKey, CancellationToken ct)
    {
        var profile = await _profileStore.GetAsync(tenant, userKey, ct);

        if (profile is null || profile.IsDeleted)
            throw new InvalidOperationException("user_profile_not_found");

        var identifiers = await _identifierStore.GetByUserAsync(tenant, userKey, ct);

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

    private static bool IsSelfTransitionAllowed(UserStatus from, UserStatus to)
        => (from, to) switch
        {
            (UserStatus.Active, UserStatus.SelfSuspended) => true,
            (UserStatus.SelfSuspended, UserStatus.Active) => true,
            (UserStatus.Active or UserStatus.SelfSuspended, UserStatus.Deactivated) => true,
            _ => false
        };

    private static bool IsLoginIdentifier(UserIdentifierType type)
        => type is
            UserIdentifierType.Username or
            UserIdentifierType.Email or
            UserIdentifierType.Phone;

}
