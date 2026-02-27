using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class UserApplicationService : IUserApplicationService
{
    private readonly IAccessOrchestrator _accessOrchestrator;
    private readonly IUserLifecycleStore _lifecycleStore;
    private readonly IUserProfileStore _profileStore;
    private readonly IUserIdentifierStore _identifierStore;
    private readonly IEnumerable<IUserLifecycleIntegration> _integrations;
    private readonly IIdentifierNormalizer _identifierNormalizer;
    private readonly UAuthServerOptions _options;
    private readonly IClock _clock;

    public UserApplicationService(
        IAccessOrchestrator accessOrchestrator,
        IUserLifecycleStore lifecycleStore,
        IUserProfileStore profileStore,
        IUserIdentifierStore identifierStore,
        IEnumerable<IUserLifecycleIntegration> integrations,
        IIdentifierNormalizer identifierNormalizer,
        IOptions<UAuthServerOptions> options,
        IClock clock)
    {
        _accessOrchestrator = accessOrchestrator;
        _lifecycleStore = lifecycleStore;
        _profileStore = profileStore;
        _identifierStore = identifierStore;
        _integrations = integrations;
        _identifierNormalizer = identifierNormalizer;
        _options = options.Value;
        _clock = clock;
    }

    #region User Lifecycle

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

    #endregion


    #region User Profile

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

    #endregion


    #region Identifiers

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
            return identifier is null ? null : UserIdentifierMapper.ToDto(identifier);
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task<bool> UserIdentifierExistsAsync(AccessContext context, UserIdentifierType type, string value, IdentifierExistenceScope scope = IdentifierExistenceScope.TenantPrimaryOnly, CancellationToken ct = default)
    {
        var command = new UserIdentifierExistsCommand(async innerCt =>
        {
            var normalized = _identifierNormalizer.Normalize(type, value);
            if (!normalized.IsValid)
                return false;

            var result = await _identifierStore.ExistsAsync(new IdentifierExistenceQuery(context.ResourceTenant, type, normalized.Normalized, scope), innerCt);
            return result.Exists;
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task AddUserIdentifierAsync(AccessContext context, AddUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new AddUserIdentifierCommand(async innerCt =>
        {
            EnsureOverrideAllowed(context);
            var userKey = context.GetTargetUserKey();

            var normalized = _identifierNormalizer.Normalize(request.Type, request.Value);
            if (!normalized.IsValid)
                throw new UAuthIdentifierValidationException(normalized.ErrorCode ?? "identifier_invalid");

            var existing = await _identifierStore.GetByUserAsync(context.ResourceTenant, userKey, innerCt);
            EnsureMultipleIdentifierAllowed(request.Type, existing);

            var userScopeResult = await _identifierStore.ExistsAsync(
                new IdentifierExistenceQuery(context.ResourceTenant, request.Type, normalized.Normalized, IdentifierExistenceScope.WithinUser, UserKey: userKey), innerCt);

            if (userScopeResult.Exists)
                throw new UAuthIdentifierConflictException("identifier_already_exists_for_user");

            var mustBeUnique = _options.LoginIdentifiers.EnforceGlobalUniquenessForAllIdentifiers ||
                (request.IsPrimary && _options.LoginIdentifiers.AllowedTypes.Contains(request.Type));

            if (mustBeUnique)
            {
                var scope = _options.LoginIdentifiers.EnforceGlobalUniquenessForAllIdentifiers
                    ? IdentifierExistenceScope.TenantAny
                    : IdentifierExistenceScope.TenantPrimaryOnly;

                var globalResult = await _identifierStore.ExistsAsync(
                    new IdentifierExistenceQuery(
                        context.ResourceTenant,
                        request.Type,
                        normalized.Normalized,
                        scope),
                    innerCt);

                if (globalResult.Exists)
                    throw new UAuthIdentifierConflictException("identifier_already_exists");
            }

            if (request.IsPrimary)
            {
                // new identifiers are not verified by default, so we check against the requirement even if the request doesn't explicitly set it to true.
                // This prevents adding a primary identifier that doesn't meet verification requirements.
                EnsureVerificationRequirements(request.Type, isVerified: false);
            }

            await _identifierStore.CreateAsync(context.ResourceTenant,
                new UserIdentifier
                {
                    UserKey = userKey,
                    Type = request.Type,
                    Value = request.Value,
                    NormalizedValue = normalized.Normalized,
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
            EnsureOverrideAllowed(context);

            var identifier = await _identifierStore.GetByIdAsync(request.Id, innerCt);

            if (identifier is null || identifier.IsDeleted)
                throw new UAuthIdentifierNotFoundException("identifier_not_found");

            if (identifier.Type == UserIdentifierType.Username && !_options.UserIdentifiers.AllowUsernameChange)
            {
                throw new UAuthIdentifierValidationException("username_change_not_allowed");
            }

            var normalized = _identifierNormalizer.Normalize(identifier.Type, request.NewValue);
            if (!normalized.IsValid)
                throw new UAuthIdentifierValidationException(normalized.ErrorCode ?? "identifier_invalid");

            if (string.Equals(identifier.NormalizedValue, normalized.Normalized, StringComparison.Ordinal))
                throw new UAuthIdentifierValidationException("identifier_value_unchanged");

            var mustBeUnique = _options.LoginIdentifiers.EnforceGlobalUniquenessForAllIdentifiers ||
                (identifier.IsPrimary && _options.LoginIdentifiers.AllowedTypes.Contains(identifier.Type));

            if (mustBeUnique)
            {
                var scope = _options.LoginIdentifiers.EnforceGlobalUniquenessForAllIdentifiers
                    ? IdentifierExistenceScope.TenantAny
                    : IdentifierExistenceScope.TenantPrimaryOnly;

                var result = await _identifierStore.ExistsAsync(
                    new IdentifierExistenceQuery(
                        identifier.Tenant,
                        identifier.Type,
                        normalized.Normalized,
                        scope,
                        ExcludeIdentifierId: identifier.Id),
                    innerCt);

                if (result.Exists)
                    throw new UAuthIdentifierConflictException("identifier_already_exists");
            }

            await _identifierStore.UpdateValueAsync(identifier.Id, request.NewValue, normalized.Normalized, resetVerification: true, _clock.UtcNow, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task SetPrimaryUserIdentifierAsync(AccessContext context, SetPrimaryUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new SetPrimaryUserIdentifierCommand(async innerCt =>
        {
            EnsureOverrideAllowed(context);

            var identifier = await _identifierStore.GetByIdAsync(request.IdentifierId, innerCt);
            if (identifier is null)
                throw new UAuthIdentifierNotFoundException("identifier_not_found");

            if (identifier.IsPrimary)
                throw new UAuthIdentifierValidationException("identifier_already_primary");

            EnsureVerificationRequirements(identifier.Type, identifier.IsVerified);

            //var identifiers = await _identifierStore.GetByUserAsync(identifier.Tenant, identifier.UserKey, innerCt);
            //var activeIdentifiers = identifiers.Where(i => !i.IsDeleted).ToList();

            var result = await _identifierStore.ExistsAsync(
                new IdentifierExistenceQuery(identifier.Tenant, identifier.Type, identifier.NormalizedValue, IdentifierExistenceScope.TenantPrimaryOnly, ExcludeIdentifierId: identifier.Id), innerCt);

            if (result.Exists)
                throw new UAuthIdentifierConflictException("identifier_already_exists");

            //var userIdentifiers =
            //await _identifierStore.GetByUserAsync(identifier.Tenant, identifier.UserKey, innerCt);

            //var existingPrimaryOfSameType = userIdentifiers
            //    .FirstOrDefault(i =>
            //        !i.IsDeleted &&
            //        i.Type == identifier.Type &&
            //        i.IsPrimary);

            //if (existingPrimaryOfSameType is not null)
            //{
            //    await _identifierStore.UnsetPrimaryAsync(existingPrimaryOfSameType.Id, innerCt);
            //}

            await _identifierStore.SetPrimaryAsync(request.IdentifierId, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task UnsetPrimaryUserIdentifierAsync(AccessContext context, UnsetPrimaryUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new UnsetPrimaryUserIdentifierCommand(async innerCt =>
        {
            EnsureOverrideAllowed(context);

            var identifier = await _identifierStore.GetByIdAsync(request.IdentifierId, innerCt);
            if (identifier is null)
                throw new UAuthIdentifierNotFoundException("identifier_not_found");

            if (!identifier.IsPrimary)
                throw new UAuthIdentifierValidationException("identifier_not_primary");

            var userIdentifiers =
            await _identifierStore.GetByUserAsync(identifier.Tenant, identifier.UserKey, innerCt);

            var activeLoginPrimaries = userIdentifiers
                .Where(i =>
                    !i.IsDeleted &&
                    i.IsPrimary &&
                    _options.LoginIdentifiers.AllowedTypes.Contains(i.Type))
                .ToList();

            if (activeLoginPrimaries.Count == 1 &&
            activeLoginPrimaries[0].Id == identifier.Id)
            {
                throw new UAuthIdentifierConflictException("cannot_unset_last_login_identifier");
            }

            await _identifierStore.UnsetPrimaryAsync(request.IdentifierId, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task VerifyUserIdentifierAsync(AccessContext context, VerifyUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new VerifyUserIdentifierCommand(async innerCt =>
        {
            EnsureOverrideAllowed(context);
            await _identifierStore.MarkVerifiedAsync(request.IdentifierId, _clock.UtcNow, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task DeleteUserIdentifierAsync(AccessContext context, DeleteUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new DeleteUserIdentifierCommand(async innerCt =>
        {
            EnsureOverrideAllowed(context);

            var identifier = await _identifierStore.GetByIdAsync(request.IdentifierId, innerCt);
            if (identifier is null)
                throw new UAuthIdentifierNotFoundException("identifier_not_found");

            var identifiers = await _identifierStore.GetByUserAsync(identifier.Tenant, identifier.UserKey, innerCt);
            var loginIdentifiers = identifiers.Where(i => !i.IsDeleted && IsLoginIdentifier(i.Type)).ToList();

            if (identifier.IsPrimary)
                throw new UAuthIdentifierValidationException("cannot_delete_primary_identifier");

            if (_options.UserIdentifiers.RequireUsernameIdentifier && identifier.Type == UserIdentifierType.Username)
            {
                var activeUsernames = identifiers
                    .Where(i => !i.IsDeleted && i.Type == UserIdentifierType.Username)
                    .ToList();

                if (activeUsernames.Count == 1)
                    throw new UAuthIdentifierConflictException("cannot_delete_last_username_identifier");
            }

            if (IsLoginIdentifier(identifier.Type) && loginIdentifiers.Count == 1)
                throw new UAuthIdentifierConflictException("cannot_delete_last_login_identifier"); 

            await _identifierStore.DeleteAsync(request.IdentifierId, request.Mode, _clock.UtcNow, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    #endregion


    #region Helpers

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

    private void EnsureMultipleIdentifierAllowed(UserIdentifierType type, IReadOnlyList<UserIdentifier> existing)
    {
        bool hasSameType = existing.Any(i => !i.IsDeleted && i.Type == type);

        if (!hasSameType)
            return;

        if (type == UserIdentifierType.Username && !_options.UserIdentifiers.AllowMultipleUsernames)
            throw new InvalidOperationException("multiple_usernames_not_allowed");

        if (type == UserIdentifierType.Email && !_options.UserIdentifiers.AllowMultipleEmail)
            throw new InvalidOperationException("multiple_emails_not_allowed");

        if (type == UserIdentifierType.Phone && !_options.UserIdentifiers.AllowMultiplePhone)
            throw new InvalidOperationException("multiple_phones_not_allowed");
    }

    private void EnsureVerificationRequirements(UserIdentifierType type, bool isVerified)
    {
        if (type == UserIdentifierType.Email && _options.UserIdentifiers.RequireEmailVerification && !isVerified)
        {
            throw new InvalidOperationException("email_verification_required");
        }

        if (type == UserIdentifierType.Phone && _options.UserIdentifiers.RequirePhoneVerification && !isVerified)
        {
            throw new InvalidOperationException("phone_verification_required");
        }
    }

    private void EnsureOverrideAllowed(AccessContext context)
    {
        if (context.IsSelfAction && !_options.UserIdentifiers.AllowUserOverride)
            throw new InvalidOperationException("user_override_not_allowed");

        if (!context.IsSelfAction && !_options.UserIdentifiers.AllowAdminOverride)
            throw new InvalidOperationException("admin_override_not_allowed");
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

    #endregion
}
