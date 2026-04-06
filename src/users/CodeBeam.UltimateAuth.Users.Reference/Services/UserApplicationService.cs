using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class UserApplicationService : IUserApplicationService
{
    private readonly IAccessOrchestrator _accessOrchestrator;
    private readonly IUserLifecycleStoreFactory _lifecycleStoreFactory;
    private readonly IUserIdentifierStoreFactory _identifierStoreFactory;
    private readonly IUserProfileStoreFactory _profileStoreFactory;
    private readonly IUserCreateValidator _userCreateValidator;
    private readonly IIdentifierValidator _identifierValidator;
    private readonly IEnumerable<IUserLifecycleIntegration> _integrations;
    private readonly IIdentifierNormalizer _identifierNormalizer;
    private readonly ISessionStoreFactory _sessionStoreFactory;
    private readonly UAuthServerOptions _options;
    private readonly IClock _clock;

    public UserApplicationService(
        IAccessOrchestrator accessOrchestrator,
        IUserLifecycleStoreFactory lifecycleStoreFactory,
        IUserIdentifierStoreFactory identifierStoreFactory,
        IUserProfileStoreFactory profileStoreFactory,
        IUserCreateValidator userCreateValidator,
        IIdentifierValidator identifierValidator,
        IEnumerable<IUserLifecycleIntegration> integrations,
        IIdentifierNormalizer identifierNormalizer,
        ISessionStoreFactory sessionStoreFactory,
        IOptions<UAuthServerOptions> options,
        IClock clock)
    {
        _accessOrchestrator = accessOrchestrator;
        _lifecycleStoreFactory = lifecycleStoreFactory;
        _identifierStoreFactory = identifierStoreFactory;
        _profileStoreFactory = profileStoreFactory;
        _userCreateValidator = userCreateValidator;
        _identifierValidator = identifierValidator;
        _integrations = integrations;
        _identifierNormalizer = identifierNormalizer;
        _sessionStoreFactory = sessionStoreFactory;
        _options = options.Value;
        _clock = clock;
    }

    #region User Lifecycle

    public async Task<UserCreateResult> CreateUserAsync(AccessContext context, CreateUserRequest request, CancellationToken ct = default)
    {
        var command = new AccessCommand<UserCreateResult>(async innerCt =>
        {
            var validationResult = await _userCreateValidator.ValidateAsync(context, request, innerCt);
            if (validationResult.IsValid != true)
            {
                throw new UAuthValidationException(string.Join(", ", validationResult.Errors));
            }

            var now = _clock.UtcNow;
            var userKey = UserKey.New();

            var lifecycleStore = _lifecycleStoreFactory.Create(context.ResourceTenant);
            await lifecycleStore.AddAsync(UserLifecycle.Create(context.ResourceTenant, userKey, now), innerCt);

            var profileStore = _profileStoreFactory.Create(context.ResourceTenant);
            await profileStore.AddAsync(
                UserProfile.Create(
                    Guid.NewGuid(),
                    context.ResourceTenant,
                    userKey,
                    ProfileKey.Default,
                    now,
                    firstName: request.FirstName,
                    lastName: request.LastName,
                    displayName: request.DisplayName ?? request.UserName ?? request.Email ?? request.Phone,
                    birthDate: request.BirthDate,
                    gender: request.Gender,
                    bio: request.Bio,
                    language: request.Language,
                    timezone: request.TimeZone,
                    culture: request.Culture), innerCt);

            var identifierStore = _identifierStoreFactory.Create(context.ResourceTenant);
            if (!string.IsNullOrWhiteSpace(request.UserName))
            {
                await identifierStore.AddAsync(
                    UserIdentifier.Create(
                        Guid.NewGuid(),
                        context.ResourceTenant,
                        userKey,
                        UserIdentifierType.Username,
                        request.UserName,
                        _identifierNormalizer.Normalize(UserIdentifierType.Username, request.UserName).Normalized,
                        now,
                        true,
                        request.UserNameVerified ? now : null), innerCt);
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                await identifierStore.AddAsync(
                    UserIdentifier.Create(
                        Guid.NewGuid(),
                        context.ResourceTenant,
                        userKey,
                        UserIdentifierType.Email,
                        request.Email,
                        _identifierNormalizer.Normalize(UserIdentifierType.Email, request.Email).Normalized,
                        now,
                        true,
                        request.EmailVerified ? now : null), innerCt);
            }

            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                await identifierStore.AddAsync(
                    UserIdentifier.Create(
                        Guid.NewGuid(),
                        context.ResourceTenant,
                        userKey,
                        UserIdentifierType.Phone,
                        request.Phone,
                        _identifierNormalizer.Normalize(UserIdentifierType.Phone, request.Phone).Normalized,
                        now,
                        true,
                        request.PhoneVerified ? now : null), innerCt);
            }

            foreach (var integration in _integrations)
            {
                // Credential creation handle on here
                await integration.OnUserCreatedAsync(context.ResourceTenant, userKey, request, innerCt);
            }

            return UserCreateResult.Success(userKey);
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task ChangeUserStatusAsync(AccessContext context, object request, CancellationToken ct = default)
    {
        var command = new AccessCommand(async innerCt =>
        {
            var newStatus = request switch
            {
                ChangeUserStatusSelfRequest r => UserStatusMapper.ToUserStatus(r.NewStatus),
                ChangeUserStatusAdminRequest r => UserStatusMapper.ToUserStatus(r.NewStatus),
                _ => throw new InvalidOperationException("invalid_request")
            };

            var targetUserKey = context.GetTargetUserKey();
            var userLifecycleKey = new UserLifecycleKey(context.ResourceTenant, targetUserKey);
            var lifecycleStore = _lifecycleStoreFactory.Create(context.ResourceTenant);
            var current = await lifecycleStore.GetAsync(userLifecycleKey, innerCt);
            var now = _clock.UtcNow;

            if (current is null)
                throw new UAuthNotFoundException("user_not_found");

            if (context.IsSelfAction && !IsSelfTransitionAllowed(current.Status, newStatus))
                throw new UAuthConflictException("self_transition_not_allowed");

            if (!context.IsSelfAction)
            {
                if (newStatus is UserStatus.SelfSuspended)
                    throw new UAuthConflictException("admin_cannot_set_self_status");
            }
            var newEntity = current.ChangeStatus(now, newStatus);
            await lifecycleStore.SaveAsync(newEntity, current.Version, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task DeleteMeAsync(AccessContext context, CancellationToken ct = default)
    {
        var command = new AccessCommand(async innerCt =>
        {
            var userKey = context.GetTargetUserKey();
            var now = _clock.UtcNow;

            var lifecycleKey = new UserLifecycleKey(context.ResourceTenant, userKey);
            var lifecycleStore = _lifecycleStoreFactory.Create(context.ResourceTenant);
            var lifecycle = await lifecycleStore.GetAsync(lifecycleKey, innerCt);

            if (lifecycle is null)
                throw new UAuthNotFoundException();

            var profileStore = _profileStoreFactory.Create(context.ResourceTenant);
            var identifierStore = _identifierStoreFactory.Create(context.ResourceTenant);

            await lifecycleStore.DeleteAsync(lifecycleKey, lifecycle.Version, DeleteMode.Soft, now, innerCt);
            await identifierStore.DeleteByUserAsync(userKey, DeleteMode.Soft, now, innerCt);

            var profiles = await profileStore.GetAllProfilesByUserAsync(userKey, innerCt);

            foreach (var profile in profiles)
            {
                var key = new UserProfileKey(context.ResourceTenant, userKey, profile.ProfileKey);
                await profileStore.DeleteAsync(key, profile.Version, DeleteMode.Soft, now, innerCt);
            }

            foreach (var integration in _integrations)
            {
                await integration.OnUserDeletedAsync(context.ResourceTenant, userKey, DeleteMode.Soft, innerCt);
            }

            var sessionStore = _sessionStoreFactory.Create(context.ResourceTenant);
            await sessionStore.RevokeAllChainsAsync(userKey, now, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task DeleteUserAsync(AccessContext context, DeleteUserRequest request, CancellationToken ct = default)
    {
        var command = new AccessCommand(async innerCt =>
        {
            var targetUserKey = context.GetTargetUserKey();
            var now = _clock.UtcNow;
            var userLifecycleKey = new UserLifecycleKey(context.ResourceTenant, targetUserKey);
            var lifecycleStore = _lifecycleStoreFactory.Create(context.ResourceTenant);
            var lifecycle = await lifecycleStore.GetAsync(userLifecycleKey, innerCt);

            if (lifecycle is null)
                throw new UAuthNotFoundException();

            var profileStore = _profileStoreFactory.Create(context.ResourceTenant);
            var identifierStore = _identifierStoreFactory.Create(context.ResourceTenant);
            await lifecycleStore.DeleteAsync(userLifecycleKey, lifecycle.Version, request.Mode, now, innerCt);
            await identifierStore.DeleteByUserAsync(targetUserKey, request.Mode, now, innerCt);

            var profiles = await profileStore.GetAllProfilesByUserAsync(targetUserKey, innerCt);

            foreach (var profile in profiles)
            {
                var key = new UserProfileKey(context.ResourceTenant, profile.UserKey, profile.ProfileKey);
                await profileStore.DeleteAsync(key, profile.Version, DeleteMode.Soft, now, innerCt);
            }

            foreach (var integration in _integrations)
            {
                await integration.OnUserDeletedAsync(context.ResourceTenant, targetUserKey, request.Mode, innerCt);
            }
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    #endregion


    #region User Profile

    public async Task<UserView> GetMeAsync(AccessContext context, ProfileKey? profileKey = null, CancellationToken ct = default)
    {
        var command = new AccessCommand<UserView>(async innerCt =>
        {
            var effectiveProfileKey = profileKey ?? ProfileKey.Default;

            if (context.ActorUserKey is null)
                throw new UnauthorizedAccessException();

            if (!_options.UserProfile.EnableMultiProfile && effectiveProfileKey != ProfileKey.Default)
                throw new UAuthConflictException("multi_profile_disabled");

            return await BuildUserViewAsync(context.ResourceTenant, context.ActorUserKey.Value, effectiveProfileKey, innerCt);
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task<UserView> GetUserProfileAsync(AccessContext context, ProfileKey? profileKey = null, CancellationToken ct = default)
    {
        var command = new AccessCommand<UserView>(async innerCt =>
        {
            var effectiveProfileKey = profileKey ?? ProfileKey.Default;

            if (!_options.UserProfile.EnableMultiProfile && effectiveProfileKey != ProfileKey.Default)
                throw new UAuthConflictException("multi_profile_disabled");

            var targetUserKey = context.GetTargetUserKey();
            return await BuildUserViewAsync(context.ResourceTenant, targetUserKey, effectiveProfileKey, innerCt);

        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task CreateProfileAsync(AccessContext context, CreateProfileRequest request, CancellationToken ct = default)
    {
        var command = new AccessCommand(async innerCt =>
        {
            var tenant = context.ResourceTenant;
            var userKey = context.GetTargetUserKey();
            var now = _clock.UtcNow;

            var profileKey = request.ProfileKey;

            if (!_options.UserProfile.EnableMultiProfile)
                throw new UAuthConflictException("multi_profile_disabled");

            if (profileKey == ProfileKey.Default)
                throw new UAuthConflictException("default_profile_already_exists");

            var store = _profileStoreFactory.Create(tenant);

            var exists = await store.ExistsAsync(new UserProfileKey(tenant, userKey, profileKey), innerCt);

            if (exists)
                throw new UAuthConflictException("profile_already_exists");

            UserProfile profile;
            if (request.CloneFrom is ProfileKey cloneFromKey)
            {
                var source = await store.GetAsync(new UserProfileKey(tenant, userKey, cloneFromKey), innerCt);

                if (source == null)
                    throw new UAuthNotFoundException("source_profile_not_found");

                profile = source.CloneTo(Guid.NewGuid(), profileKey, now);
            }
            else
            {
                profile = UserProfile.Create(
                    Guid.NewGuid(),
                    tenant,
                    userKey,
                    profileKey,
                    now,
                    firstName: request.FirstName,
                    lastName: request.LastName,
                    displayName: request.DisplayName,
                    birthDate: request.BirthDate,
                    gender: request.Gender,
                    bio: request.Bio,
                    language: request.Language,
                    timezone: request.TimeZone,
                    culture: request.Culture);
            }

            await store.AddAsync(profile, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task UpdateUserProfileAsync(AccessContext context, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var command = new AccessCommand(async innerCt =>
        {
            var tenant = context.ResourceTenant;
            var userKey = context.GetTargetUserKey();
            var now = _clock.UtcNow;

            var profileKey = request.ProfileKey ?? ProfileKey.Default;
            var key = new UserProfileKey(tenant, userKey, profileKey);
            var profileStore = _profileStoreFactory.Create(tenant);
            var profile = await profileStore.GetAsync(key, innerCt);

            if (profile is null)
                throw new UAuthNotFoundException();

            if (!_options.UserProfile.EnableMultiProfile && profileKey != ProfileKey.Default)
                throw new UAuthConflictException("multi_profile_disabled");

            var expectedVersion = profile.Version;

            profile
                .UpdateName(request.FirstName, request.LastName, request.DisplayName, now)
                .UpdatePersonalInfo(request.BirthDate, request.Gender, request.Bio, now)
                .UpdateLocalization(request.Language, request.TimeZone, request.Culture, now)
                .UpdateMetadata(request.Metadata, now);

            await profileStore.SaveAsync(profile, expectedVersion, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task DeleteProfileAsync(AccessContext context, ProfileKey profileKey, CancellationToken ct = default)
    {
        var command = new AccessCommand(async innerCt =>
        {
            var tenant = context.ResourceTenant;
            var userKey = context.GetTargetUserKey();
            var now = _clock.UtcNow;

            if (!_options.UserProfile.EnableMultiProfile)
                throw new UAuthConflictException("multi_profile_disabled");

            if (profileKey == ProfileKey.Default)
                throw new UAuthConflictException("cannot_delete_default_profile");

            var store = _profileStoreFactory.Create(tenant);

            var key = new UserProfileKey(tenant, userKey, profileKey);
            var profile = await store.GetAsync(key, innerCt);

            if (profile is null || profile.IsDeleted)
                throw new UAuthNotFoundException("user_profile_not_found");

            var profiles = await store.GetAllProfilesByUserAsync(userKey, innerCt);

            if (profiles.Count <= 1)
                throw new UAuthConflictException("cannot_delete_last_profile");

            await store.DeleteAsync(key, profile.Version, DeleteMode.Soft, now, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    #endregion


    #region Identifiers

    public async Task<PagedResult<UserIdentifierInfo>> GetIdentifiersByUserAsync(AccessContext context, UserIdentifierQuery query, CancellationToken ct = default)
    {
        var command = new AccessCommand<PagedResult<UserIdentifierInfo>>(async innerCt =>
        {
            var targetUserKey = context.GetTargetUserKey();

            query ??= new UserIdentifierQuery();
            query = query with
            {
                UserKey = targetUserKey
            };
            var identifierStore = _identifierStoreFactory.Create(context.ResourceTenant);
            var result = await identifierStore.QueryAsync(query, innerCt);
            var dtoItems = result.Items.Select(UserIdentifierMapper.ToDto).ToList().AsReadOnly();

            return new PagedResult<UserIdentifierInfo>(
                dtoItems,
                result.TotalCount,
                result.PageNumber,
                result.PageSize,
                result.SortBy,
                result.Descending);
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task<UserIdentifierInfo?> GetIdentifierAsync(AccessContext context, UserIdentifierType type, string value, CancellationToken ct = default)
    {
        var command = new AccessCommand<UserIdentifierInfo?>(async innerCt =>
        {
            var normalized = _identifierNormalizer.Normalize(type, value);
            if (!normalized.IsValid)
                return null;

            var identifierStore = _identifierStoreFactory.Create(context.ResourceTenant);
            var identifier = await identifierStore.GetAsync(type, normalized.Normalized, innerCt);
            return identifier is null ? null : UserIdentifierMapper.ToDto(identifier);
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task<bool> UserIdentifierExistsAsync(AccessContext context, UserIdentifierType type, string value, IdentifierExistenceScope scope = IdentifierExistenceScope.TenantPrimaryOnly, CancellationToken ct = default)
    {
        var command = new AccessCommand<bool>(async innerCt =>
        {
            var normalized = _identifierNormalizer.Normalize(type, value);
            if (!normalized.IsValid)
                return false;

            UserKey? userKey = scope == IdentifierExistenceScope.WithinUser ? context.GetTargetUserKey() : null;

            var identifierStore = _identifierStoreFactory.Create(context.ResourceTenant);
            var result = await identifierStore.ExistsAsync(new IdentifierExistenceQuery(type, normalized.Normalized, scope, userKey), innerCt);
            return result.Exists;
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task AddUserIdentifierAsync(AccessContext context, AddUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new AccessCommand(async innerCt =>
        {
            var validationDto = new UserIdentifierInfo() { Type = request.Type, Value = request.Value };
            var validationResult = await _identifierValidator.ValidateAsync(context, validationDto, innerCt);
            if (validationResult.IsValid != true)
            {
                throw new UAuthValidationException(string.Join(", ", validationResult.Errors));
            }

            EnsureOverrideAllowed(context);
            var userKey = context.GetTargetUserKey();

            var normalized = _identifierNormalizer.Normalize(request.Type, request.Value);
            if (!normalized.IsValid)
                throw new UAuthIdentifierValidationException(normalized.ErrorCode ?? "identifier_invalid");

            var identifierStore = _identifierStoreFactory.Create(context.ResourceTenant);
            var existing = await identifierStore.GetByUserAsync(userKey, innerCt);
            EnsureMultipleIdentifierAllowed(request.Type, existing);

            var userScopeResult = await identifierStore.ExistsAsync(
                new IdentifierExistenceQuery(request.Type, normalized.Normalized, IdentifierExistenceScope.WithinUser, UserKey: userKey), innerCt);

            if (userScopeResult.Exists)
                throw new UAuthIdentifierConflictException("identifier_already_exists_for_user");

            var mustBeUnique = _options.LoginIdentifiers.EnforceGlobalUniquenessForAllIdentifiers ||
                (request.IsPrimary && _options.LoginIdentifiers.AllowedTypes.Contains(request.Type));

            if (mustBeUnique)
            {
                var scope = _options.LoginIdentifiers.EnforceGlobalUniquenessForAllIdentifiers
                    ? IdentifierExistenceScope.TenantAny
                    : IdentifierExistenceScope.TenantPrimaryOnly;

                var globalResult = await identifierStore.ExistsAsync(
                    new IdentifierExistenceQuery(
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

            await identifierStore.AddAsync(
                    UserIdentifier.Create(
                        Guid.NewGuid(),
                        context.ResourceTenant,
                        userKey,
                        request.Type,
                        request.Value,
                        normalized.Normalized,
                        _clock.UtcNow,
                        request.IsPrimary), innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task UpdateUserIdentifierAsync(AccessContext context, UpdateUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new AccessCommand(async innerCt =>
        {
            EnsureOverrideAllowed(context);

            var identifierStore = _identifierStoreFactory.Create(context.ResourceTenant);
            var identifier = await identifierStore.GetByIdAsync(request.Id, innerCt);

            if (identifier is null || identifier.IsDeleted)
                throw new UAuthIdentifierNotFoundException("identifier_not_found");

            if (identifier.Type == UserIdentifierType.Username && !_options.Identifiers.AllowUsernameChange)
            {
                throw new UAuthIdentifierValidationException("username_change_not_allowed");
            }

            var validationDto = identifier.ToDto();
            var validationResult = await _identifierValidator.ValidateAsync(context, validationDto, innerCt);
            if (validationResult.IsValid != true)
            {
                throw new UAuthValidationException(string.Join(", ", validationResult.Errors));
            }

            var normalized = _identifierNormalizer.Normalize(identifier.Type, request.NewValue);
            if (!normalized.IsValid)
                throw new UAuthIdentifierValidationException(normalized.ErrorCode ?? "identifier_invalid");

            if (string.Equals(identifier.NormalizedValue, normalized.Normalized, StringComparison.Ordinal))
                throw new UAuthIdentifierValidationException("identifier_value_unchanged");

            var withinUserResult = await identifierStore.ExistsAsync(
                new IdentifierExistenceQuery(
                    identifier.Type,
                    normalized.Normalized,
                    IdentifierExistenceScope.WithinUser,
                    UserKey: identifier.UserKey,
                    ExcludeIdentifierId: identifier.Id),
                innerCt);

            if (withinUserResult.Exists)
                throw new UAuthIdentifierConflictException("identifier_already_exists_for_user");

            var mustBeUnique = _options.LoginIdentifiers.EnforceGlobalUniquenessForAllIdentifiers ||
                (identifier.IsPrimary && _options.LoginIdentifiers.AllowedTypes.Contains(identifier.Type));

            if (mustBeUnique)
            {
                var scope = _options.LoginIdentifiers.EnforceGlobalUniquenessForAllIdentifiers
                    ? IdentifierExistenceScope.TenantAny
                    : IdentifierExistenceScope.TenantPrimaryOnly;

                var result = await identifierStore.ExistsAsync(
                    new IdentifierExistenceQuery(
                        identifier.Type,
                        normalized.Normalized,
                        scope,
                        ExcludeIdentifierId: identifier.Id),
                    innerCt);

                if (result.Exists)
                    throw new UAuthIdentifierConflictException("identifier_already_exists");
            }

            var expectedVersion = identifier.Version;
            identifier.ChangeValue(request.NewValue, normalized.Normalized, _clock.UtcNow);

            await identifierStore.SaveAsync(identifier, expectedVersion, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task SetPrimaryUserIdentifierAsync(AccessContext context, SetPrimaryUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new AccessCommand(async innerCt =>
        {
            EnsureOverrideAllowed(context);

            var identifierStore = _identifierStoreFactory.Create(context.ResourceTenant);
            var identifier = await identifierStore.GetByIdAsync(request.Id, innerCt);
            if (identifier is null)
                throw new UAuthIdentifierNotFoundException("identifier_not_found");

            if (identifier.IsPrimary)
                throw new UAuthIdentifierValidationException("identifier_already_primary");

            EnsureVerificationRequirements(identifier.Type, identifier.IsVerified);

            var result = await identifierStore.ExistsAsync(
                new IdentifierExistenceQuery(identifier.Type, identifier.NormalizedValue, IdentifierExistenceScope.TenantPrimaryOnly, ExcludeIdentifierId: identifier.Id), innerCt);

            if (result.Exists)
                throw new UAuthIdentifierConflictException("identifier_already_exists");

            var expectedVersion = identifier.Version;
            identifier.SetPrimary(_clock.UtcNow);
            await identifierStore.SaveAsync(identifier, expectedVersion, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task UnsetPrimaryUserIdentifierAsync(AccessContext context, UnsetPrimaryUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new AccessCommand(async innerCt =>
        {
            EnsureOverrideAllowed(context);

            var identifierStore = _identifierStoreFactory.Create(context.ResourceTenant);
            var identifier = await identifierStore.GetByIdAsync(request.Id, innerCt);
            if (identifier is null)
                throw new UAuthIdentifierNotFoundException("identifier_not_found");

            if (!identifier.IsPrimary)
                throw new UAuthIdentifierValidationException("identifier_already_not_primary");

            var userIdentifiers =
            await identifierStore.GetByUserAsync(identifier.UserKey, innerCt);

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

            var expectedVersion = identifier.Version;
            identifier.UnsetPrimary(_clock.UtcNow);
            await identifierStore.SaveAsync(identifier, expectedVersion, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task VerifyUserIdentifierAsync(AccessContext context, VerifyUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new AccessCommand(async innerCt =>
        {
            EnsureOverrideAllowed(context);

            var identifierStore = _identifierStoreFactory.Create(context.ResourceTenant);
            var identifier = await identifierStore.GetByIdAsync(request.Id, innerCt);
            if (identifier is null)
                throw new UAuthIdentifierNotFoundException("identifier_not_found");

            var expectedVersion = identifier.Version;
            identifier.MarkVerified(_clock.UtcNow);
            await identifierStore.SaveAsync(identifier, expectedVersion, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task DeleteUserIdentifierAsync(AccessContext context, DeleteUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new AccessCommand(async innerCt =>
        {
            EnsureOverrideAllowed(context);

            var identifierStore = _identifierStoreFactory.Create(context.ResourceTenant);
            var identifier = await identifierStore.GetByIdAsync(request.Id, innerCt);
            if (identifier is null)
                throw new UAuthIdentifierNotFoundException("identifier_not_found");

            var identifiers = await identifierStore.GetByUserAsync(identifier.UserKey, innerCt);
            var loginIdentifiers = identifiers.Where(i => !i.IsDeleted && IsLoginIdentifier(i.Type)).ToList();

            if (identifier.IsPrimary)
                throw new UAuthIdentifierValidationException("cannot_delete_primary_identifier");

            if (_options.Identifiers.RequireUsernameIdentifier && identifier.Type == UserIdentifierType.Username)
            {
                var activeUsernames = identifiers
                    .Where(i => !i.IsDeleted && i.Type == UserIdentifierType.Username)
                    .ToList();

                if (activeUsernames.Count == 1)
                    throw new UAuthIdentifierConflictException("cannot_delete_last_username_identifier");
            }

            if (IsLoginIdentifier(identifier.Type) && loginIdentifiers.Count == 1)
                throw new UAuthIdentifierConflictException("cannot_delete_last_login_identifier");

            var expectedVersion = identifier.Version;

            if (request.Mode == DeleteMode.Hard)
            {
                await identifierStore.DeleteAsync(identifier.Id, expectedVersion, DeleteMode.Hard, _clock.UtcNow, innerCt);
            }
            else
            {
                identifier.MarkDeleted(_clock.UtcNow);
                await identifierStore.SaveAsync(identifier, expectedVersion, innerCt);
            }
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    #endregion


    #region Helpers

    private async Task<UserView> BuildUserViewAsync(TenantKey tenant, UserKey userKey, ProfileKey? profileKey, CancellationToken ct)
    {
        var effectiveProfileKey = profileKey ?? ProfileKey.Default;

        var lifecycleStore = _lifecycleStoreFactory.Create(tenant);
        var identifierStore = _identifierStoreFactory.Create(tenant);
        var profileStore = _profileStoreFactory.Create(tenant);
        var lifecycle = await lifecycleStore.GetAsync(new UserLifecycleKey(tenant, userKey));
        var profile = await profileStore.GetAsync(new UserProfileKey(tenant, userKey, effectiveProfileKey), ct);

        if (lifecycle is null || lifecycle.IsDeleted)
            throw new UAuthNotFoundException("user_not_found");

        if (profile is null || profile.IsDeleted)
            throw new UAuthNotFoundException("user_profile_not_found");

        var identifiers = await identifierStore.GetByUserAsync(userKey, ct);

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
            PhoneVerified = primaryPhone?.IsVerified ?? false,
            Status = lifecycle.Status
        };
    }

    private void EnsureMultipleIdentifierAllowed(UserIdentifierType type, IReadOnlyList<UserIdentifier> existing)
    {
        bool hasSameType = existing.Any(i => !i.IsDeleted && i.Type == type);

        if (!hasSameType)
            return;

        if (type == UserIdentifierType.Username && !_options.Identifiers.AllowMultipleUsernames)
            throw new UAuthValidationException("multiple_usernames_not_allowed");

        if (type == UserIdentifierType.Email && !_options.Identifiers.AllowMultipleEmail)
            throw new UAuthValidationException("multiple_emails_not_allowed");

        if (type == UserIdentifierType.Phone && !_options.Identifiers.AllowMultiplePhone)
            throw new UAuthValidationException("multiple_phones_not_allowed");
    }

    private void EnsureVerificationRequirements(UserIdentifierType type, bool isVerified)
    {
        if (type == UserIdentifierType.Email && _options.Identifiers.RequireEmailVerification && !isVerified)
        {
            throw new UAuthValidationException("email_verification_required");
        }

        if (type == UserIdentifierType.Phone && _options.Identifiers.RequirePhoneVerification && !isVerified)
        {
            throw new UAuthValidationException("phone_verification_required");
        }
    }

    private void EnsureOverrideAllowed(AccessContext context)
    {
        if (context.IsSelfAction && !_options.Identifiers.AllowUserOverride)
            throw new UAuthConflictException("user_override_not_allowed");

        if (!context.IsSelfAction && !_options.Identifiers.AllowAdminOverride)
            throw new UAuthConflictException("admin_override_not_allowed");
    }

    private static bool IsSelfTransitionAllowed(UserStatus from, UserStatus to)
        => (from, to) switch
        {
            (UserStatus.Active, UserStatus.SelfSuspended) => true,
            (UserStatus.SelfSuspended, UserStatus.Active) => true,
            _ => false
        };

    private static bool IsLoginIdentifier(UserIdentifierType type)
        => type is
            UserIdentifierType.Username or
            UserIdentifierType.Email or
            UserIdentifierType.Phone;

    #endregion

    public async Task<PagedResult<UserSummary>> QueryUsersAsync(AccessContext context, UserQuery query, CancellationToken ct = default)
    {
        var command = new AccessCommand<PagedResult<UserSummary>>(async innerCt =>
        {
            query ??= new UserQuery();
            var effectiveProfileKey = query.ProfileKey ?? ProfileKey.Default;

            var lifecycleQuery = new UserLifecycleQuery
            {
                PageNumber = 1,
                PageSize = int.MaxValue,
                Status = query.Status,
                IncludeDeleted = query.IncludeDeleted
            };

            var lifecycleStore = _lifecycleStoreFactory.Create(context.ResourceTenant);
            var lifecycleResult = await lifecycleStore.QueryAsync(lifecycleQuery, innerCt);
            var lifecycles = lifecycleResult.Items;

            if (lifecycles.Count == 0)
            {
                return new PagedResult<UserSummary>(
                    Array.Empty<UserSummary>(),
                    0,
                    query.PageNumber,
                    query.PageSize,
                    query.SortBy,
                    query.Descending);
            }

            var userKeys = lifecycles.Select(x => x.UserKey).ToList();
            var profileStore = _profileStoreFactory.Create(context.ResourceTenant);
            var identifierStore = _identifierStoreFactory.Create(context.ResourceTenant);
            var profiles = await profileStore.GetByUsersAsync(userKeys, effectiveProfileKey, innerCt);
            var identifiers = await identifierStore.GetByUsersAsync(userKeys, innerCt);
            var profileMap = profiles.ToDictionary(x => x.UserKey);
            var identifierGroups = identifiers.GroupBy(x => x.UserKey).ToDictionary(x => x.Key, x => x.ToList());

            var summaries = new List<UserSummary>();

            foreach (var lifecycle in lifecycles)
            {
                profileMap.TryGetValue(lifecycle.UserKey, out var profile);

                identifierGroups.TryGetValue(lifecycle.UserKey, out var ids);

                var username = ids?.FirstOrDefault(x =>
                    x.Type == UserIdentifierType.Username &&
                    x.IsPrimary);

                var email = ids?.FirstOrDefault(x =>
                    x.Type == UserIdentifierType.Email &&
                    x.IsPrimary);

                summaries.Add(new UserSummary
                {
                    UserKey = lifecycle.UserKey,
                    DisplayName = profile?.DisplayName,
                    UserName = username?.Value,
                    PrimaryEmail = email?.Value,
                    Status = lifecycle.Status,
                    CreatedAt = lifecycle.CreatedAt
                });
            }

            // SEARCH
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToLowerInvariant();

                summaries = summaries
                    .Where(x =>
                        (x.DisplayName?.ToLowerInvariant().Contains(search) ?? false) ||
                        (x.PrimaryEmail?.ToLowerInvariant().Contains(search) ?? false) ||
                        (x.UserName?.ToLowerInvariant().Contains(search) ?? false) ||
                        x.UserKey.Value.ToLowerInvariant().Contains(search))
                    .ToList();
            }

            // SORT
            summaries = query.SortBy switch
            {
                nameof(UserSummary.DisplayName) =>
                    query.Descending
                        ? summaries.OrderByDescending(x => x.DisplayName).ToList()
                        : summaries.OrderBy(x => x.DisplayName).ToList(),

                nameof(UserSummary.CreatedAt) =>
                    query.Descending
                        ? summaries.OrderByDescending(x => x.CreatedAt).ToList()
                        : summaries.OrderBy(x => x.CreatedAt).ToList(),

                _ => summaries.OrderBy(x => x.CreatedAt).ToList()
            };

            var total = summaries.Count;

            // PAGINATION
            var items = summaries
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PagedResult<UserSummary>(
                items,
                total,
                query.PageNumber,
                query.PageSize,
                query.SortBy,
                query.Descending);
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }
}
