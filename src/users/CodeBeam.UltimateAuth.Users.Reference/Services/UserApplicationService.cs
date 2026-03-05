using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;
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
    private readonly ICredentialStore _credentialStore;
    private readonly IUserCreateValidator _userCreateValidator;
    private readonly IIdentifierValidator _identifierValidator;
    private readonly IEnumerable<IUserLifecycleIntegration> _integrations;
    private readonly IIdentifierNormalizer _identifierNormalizer;
    private readonly IUAuthPasswordHasher _passwordHasher;
    private readonly UAuthServerOptions _options;
    private readonly IClock _clock;

    public UserApplicationService(
        IAccessOrchestrator accessOrchestrator,
        IUserLifecycleStore lifecycleStore,
        IUserProfileStore profileStore,
        IUserIdentifierStore identifierStore,
        ICredentialStore credentialStore,
        IUserCreateValidator userCreateValidator,
        IIdentifierValidator identifierValidator,
        IEnumerable<IUserLifecycleIntegration> integrations,
        IIdentifierNormalizer identifierNormalizer,
        IUAuthPasswordHasher passwordHasher,
        IOptions<UAuthServerOptions> options,
        IClock clock)
    {
        _accessOrchestrator = accessOrchestrator;
        _lifecycleStore = lifecycleStore;
        _profileStore = profileStore;
        _identifierStore = identifierStore;
        _credentialStore = credentialStore;
        _userCreateValidator = userCreateValidator;
        _identifierValidator = identifierValidator;
        _integrations = integrations;
        _identifierNormalizer = identifierNormalizer;
        _passwordHasher = passwordHasher;
        _options = options.Value;
        _clock = clock;
    }

    #region User Lifecycle

    public async Task<UserCreateResult> CreateUserAsync(AccessContext context, CreateUserRequest request, CancellationToken ct = default)
    {
        var command = new CreateUserCommand(async innerCt =>
        {
            var validationResult = await _userCreateValidator.ValidateAsync(context, request, ct);
            if (validationResult.IsValid != true)
            {
                throw new UAuthValidationException(string.Join(", ", validationResult.Errors));
            }

            var now = _clock.UtcNow;
            var userKey = UserKey.New();

            if (string.IsNullOrWhiteSpace(request.UserName) && string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
            {
                throw new UAuthValidationException("identifier_required");
            }

            await _lifecycleStore.CreateAsync(UserLifecycle.Create(context.ResourceTenant, userKey, now), innerCt);

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                var hash = _passwordHasher.Hash(request.Password);

                await _credentialStore.AddAsync(
                    context.ResourceTenant,
                    PasswordCredential.Create(null, context.ResourceTenant, userKey, hash, CredentialSecurityState.Active(), new CredentialMetadata(), now),
                    innerCt);            
            }

            await _profileStore.CreateAsync(context.ResourceTenant,
                new UserProfile
                {
                    Tenant = context.ResourceTenant,
                    UserKey = userKey,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    DisplayName = request.DisplayName ?? request.UserName ?? request.Email,
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

            if (!string.IsNullOrWhiteSpace(request.UserName))
            {
                await _identifierStore.CreateAsync(context.ResourceTenant,
                    new UserIdentifier
                    {
                        Tenant = context.ResourceTenant,
                        UserKey = userKey,
                        Type = UserIdentifierType.Username,
                        Value = request.UserName,
                        NormalizedValue = _identifierNormalizer.Normalize(UserIdentifierType.Username, request.UserName).Normalized,
                        IsPrimary = true,
                        IsVerified = request.UserNameVerified,
                        CreatedAt = now,
                        VerifiedAt = request.UserNameVerified ? now : null
                    },
                    innerCt);
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                await _identifierStore.CreateAsync(context.ResourceTenant,
                    new UserIdentifier
                    {
                        Tenant = context.ResourceTenant,
                        UserKey = userKey,
                        Type = UserIdentifierType.Email,
                        Value = request.Email,
                        NormalizedValue = _identifierNormalizer.Normalize(UserIdentifierType.Email, request.Email).Normalized,
                        IsPrimary = true,
                        IsVerified = request.EmailVerified,
                        CreatedAt = now,
                        VerifiedAt = request.EmailVerified ? now : null
                    },
                    innerCt);
            }

            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                await _identifierStore.CreateAsync(context.ResourceTenant,
                    new UserIdentifier
                    {
                        Tenant = context.ResourceTenant,
                        UserKey = userKey,
                        Type = UserIdentifierType.Phone,
                        Value = request.Phone,
                        NormalizedValue = _identifierNormalizer.Normalize(UserIdentifierType.Phone, request.Phone).Normalized,
                        IsPrimary = true,
                        IsVerified = request.PhoneVerified,
                        CreatedAt = now,
                        VerifiedAt = request.PhoneVerified ? now : null
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
            var userLifecycleKey = new UserLifecycleKey(context.ResourceTenant, targetUserKey);
            var current = await _lifecycleStore.GetAsync(userLifecycleKey, innerCt);
            var now = DateTimeOffset.UtcNow;

            if (current is null)
                throw new InvalidOperationException("user_not_found");

            if (context.IsSelfAction && !IsSelfTransitionAllowed(current.Status, newStatus))
                throw new InvalidOperationException("self_transition_not_allowed");

            if (!context.IsSelfAction)
            {
                if (newStatus is UserStatus.SelfSuspended or UserStatus.Deactivated)
                    throw new InvalidOperationException("admin_cannot_set_self_status");
            }
            var newEntity = current.ChangeStatus(now, newStatus);
            await _lifecycleStore.UpdateAsync(newEntity, current.Version, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task DeleteUserAsync(AccessContext context, DeleteUserRequest request, CancellationToken ct = default)
    {
        var command = new DeleteUserCommand(async innerCt =>
        {
            var targetUserKey = context.GetTargetUserKey();
            var now = _clock.UtcNow;
            var userLifecycleKey = new UserLifecycleKey(context.ResourceTenant, targetUserKey);

            await _lifecycleStore.DeleteAsync(userLifecycleKey, request.Mode, now, innerCt);
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

    public async Task<PagedResult<UserIdentifierDto>> GetIdentifiersByUserAsync(AccessContext context, UserIdentifierQuery query, CancellationToken ct = default)
    {
        var command = new GetUserIdentifiersCommand(async innerCt =>
        {
            var targetUserKey = context.GetTargetUserKey();

            query ??= new UserIdentifierQuery();
            query.UserKey = targetUserKey;

            var result = await _identifierStore.QueryAsync(context.ResourceTenant, query, innerCt);
            var dtoItems = result.Items.Select(UserIdentifierMapper.ToDto).ToList().AsReadOnly();

            return new PagedResult<UserIdentifierDto>(
                dtoItems,
                result.TotalCount,
                result.PageNumber,
                result.PageSize,
                result.SortBy,
                result.Descending);
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task<UserIdentifierDto?> GetIdentifierAsync(AccessContext context, UserIdentifierType type, string value, CancellationToken ct = default)
    {
        var command = new GetUserIdentifierCommand(async innerCt =>
        {
            var normalized = _identifierNormalizer.Normalize(type, value);
            if (!normalized.IsValid)
                return null;

            var identifier = await _identifierStore.GetAsync(context.ResourceTenant, type, normalized.Normalized, innerCt);
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

            UserKey? userKey = scope == IdentifierExistenceScope.WithinUser ? context.GetTargetUserKey() : null;

            var result = await _identifierStore.ExistsAsync(new IdentifierExistenceQuery(context.ResourceTenant, type, normalized.Normalized, scope, userKey), innerCt);
            return result.Exists;
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task AddUserIdentifierAsync(AccessContext context, AddUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new AddUserIdentifierCommand(async innerCt =>
        {
            var validationDto = new UserIdentifierDto() { Type = request.Type, Value = request.Value };
            var validationResult = await _identifierValidator.ValidateAsync(context, validationDto, ct);
            if (validationResult.IsValid != true)
            {
                throw new UAuthValidationException(string.Join(", ", validationResult.Errors));
            }

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

            if (identifier.Type == UserIdentifierType.Username && !_options.Identifiers.AllowUsernameChange)
            {
                throw new UAuthIdentifierValidationException("username_change_not_allowed");
            }

            var validationDto = identifier.ToDto();
            var validationResult = await _identifierValidator.ValidateAsync(context, validationDto, ct);
            if (validationResult.IsValid != true)
            {
                throw new UAuthValidationException(string.Join(", ", validationResult.Errors));
            }

            var normalized = _identifierNormalizer.Normalize(identifier.Type, request.NewValue);
            if (!normalized.IsValid)
                throw new UAuthIdentifierValidationException(normalized.ErrorCode ?? "identifier_invalid");

            if (string.Equals(identifier.NormalizedValue, normalized.Normalized, StringComparison.Ordinal))
                throw new UAuthIdentifierValidationException("identifier_value_unchanged");

            var withinUserResult = await _identifierStore.ExistsAsync(
                new IdentifierExistenceQuery(
                    identifier.Tenant,
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

            var expectedVersion = identifier.Version;
            identifier.ChangeValue(request.NewValue, normalized.Normalized, _clock.UtcNow);

            await _identifierStore.SaveAsync(identifier, expectedVersion, innerCt);
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

            var result = await _identifierStore.ExistsAsync(
                new IdentifierExistenceQuery(identifier.Tenant, identifier.Type, identifier.NormalizedValue, IdentifierExistenceScope.TenantPrimaryOnly, ExcludeIdentifierId: identifier.Id), innerCt);

            if (result.Exists)
                throw new UAuthIdentifierConflictException("identifier_already_exists");

            var expectedVersion = identifier.Version;
            identifier.SetPrimary(_clock.UtcNow);
            await _identifierStore.SaveAsync(identifier, expectedVersion, innerCt);
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
                throw new UAuthIdentifierValidationException("identifier_already_not_primary");

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

            var expectedVersion = identifier.Version;
            identifier.UnsetPrimary(_clock.UtcNow);
            await _identifierStore.SaveAsync(identifier, expectedVersion, innerCt);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task VerifyUserIdentifierAsync(AccessContext context, VerifyUserIdentifierRequest request, CancellationToken ct = default)
    {
        var command = new VerifyUserIdentifierCommand(async innerCt =>
        {
            EnsureOverrideAllowed(context);

            var identifier = await _identifierStore.GetByIdAsync(request.IdentifierId, innerCt);
            if (identifier is null)
                throw new UAuthIdentifierNotFoundException("identifier_not_found");

            var expectedVersion = identifier.Version;
            identifier.MarkVerified(_clock.UtcNow);
            await _identifierStore.SaveAsync(identifier, expectedVersion, innerCt);
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
                await _identifierStore.DeleteAsync(identifier, expectedVersion, DeleteMode.Hard, _clock.UtcNow, innerCt);
            }
            else
            {
                identifier.MarkDeleted(_clock.UtcNow);
                await _identifierStore.SaveAsync(identifier, expectedVersion, innerCt);
            }
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

        if (type == UserIdentifierType.Username && !_options.Identifiers.AllowMultipleUsernames)
            throw new InvalidOperationException("multiple_usernames_not_allowed");

        if (type == UserIdentifierType.Email && !_options.Identifiers.AllowMultipleEmail)
            throw new InvalidOperationException("multiple_emails_not_allowed");

        if (type == UserIdentifierType.Phone && !_options.Identifiers.AllowMultiplePhone)
            throw new InvalidOperationException("multiple_phones_not_allowed");
    }

    private void EnsureVerificationRequirements(UserIdentifierType type, bool isVerified)
    {
        if (type == UserIdentifierType.Email && _options.Identifiers.RequireEmailVerification && !isVerified)
        {
            throw new InvalidOperationException("email_verification_required");
        }

        if (type == UserIdentifierType.Phone && _options.Identifiers.RequirePhoneVerification && !isVerified)
        {
            throw new InvalidOperationException("phone_verification_required");
        }
    }

    private void EnsureOverrideAllowed(AccessContext context)
    {
        if (context.IsSelfAction && !_options.Identifiers.AllowUserOverride)
            throw new InvalidOperationException("user_override_not_allowed");

        if (!context.IsSelfAction && !_options.Identifiers.AllowAdminOverride)
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
