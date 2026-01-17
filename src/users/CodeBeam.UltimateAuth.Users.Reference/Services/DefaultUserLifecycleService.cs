using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    internal sealed class DefaultUserLifecycleService : IUserLifecycleService
    {
        private readonly IUserStore<UserKey> _users;
        private readonly IUserProfileStore _profiles;
        private readonly IUserLifecycleStore _userLifecycleStore;
        private readonly IUserCredentialsService _credentials;
        private readonly ISessionService _sessionService;
        private readonly IAuthContextFactory _authContextFactory;
        private readonly IClock _clock;

        public DefaultUserLifecycleService(
            IUserStore<UserKey> users,
            IUserProfileStore profiles,
            IUserLifecycleStore userLifecycleStore,
            IUserCredentialsService credentials,
            ISessionService sessionService,
            IAuthContextFactory authContextFactory,
            IClock clock)
        {
            _users = users;
            _profiles = profiles;
            _userLifecycleStore = userLifecycleStore;
            _credentials = credentials;
            _sessionService = sessionService;
            _authContextFactory = authContextFactory;
            _clock = clock;
        }

        public async Task<UserCreateResult> CreateAsync(string? tenantId, CreateUserRequest request, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request.Identifier))
                return UserCreateResult.Failed("Identifier is required.");

            // Aynı login ile user var mı?
            var existing = await _users.FindByLoginAsync(tenantId, request.Identifier, ct);
            if (existing is not null)
                return UserCreateResult.Failed("User already exists.");

            var userKey = UserKey.New();

            // Core user record
            var record = new UserRecord<UserKey>
            {
                Id = userKey,
                Identifier = request.Identifier,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = _clock.UtcNow
            };

            await _userLifecycleStore.CreateAsync(tenantId, record, ct);

            // Reference profile
            await _profiles.UpdateAsync(
                tenantId,
                userKey,
                new UpdateProfileRequest
                {
                    DisplayName = request.DisplayName,
                },
                ct);

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                await _credentials.SetInitialAsync(tenantId, userKey,
                    new SetInitialCredentialRequest
                    {
                        Type = CredentialType.Password,
                        Secret = request.Password
                    });
            }

            return UserCreateResult.Success(userKey);
        }

        public async Task<UserStatusChangeResult> ChangeStatusAsync(string? tenantId, UserKey userKey, UserStatus newStatus, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var exists = await _users.ExistsAsync(tenantId, userKey, ct);
            if (!exists)
                return UserStatusChangeResult.NotFound();

            var profile = await _profiles.GetAsync(tenantId, userKey, ct);
            if (profile is null)
                return UserStatusChangeResult.NotFound();

            var oldStatus = profile.Status;
            if (oldStatus == newStatus)
                return UserStatusChangeResult.NoChange(oldStatus);

            await _profiles.SetStatusAsync(tenantId, userKey, newStatus, ct);

            // TODO: Check all status
            if (newStatus is UserStatus.Disabled or UserStatus.Suspended)
            {
                await _credentials.RevokeAllAsync(tenantId, userKey, ct);
            }

            return UserStatusChangeResult.Success(oldStatus, newStatus);
        }

        public async Task<UserDeleteResult> DeleteAsync(string? tenantId, UserKey userKey, UserDeleteMode mode = UserDeleteMode.Soft, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var user = await _users.FindByIdAsync(tenantId, userKey, ct);
            if (user is null)
                return UserDeleteResult.NotFound();

            var authContext = _authContextFactory.Create();
            if (mode == UserDeleteMode.Soft)
            {
                if (user.IsDeleted)
                    return UserDeleteResult.AlreadyDeleted(UserDeleteMode.Soft);

                await _userLifecycleStore.MarkDeletedAsync(tenantId, userKey, _clock.UtcNow, ct);

                await _credentials.RevokeAllAsync(tenantId, userKey, ct);
                await _sessionService.RevokeAllAsync(authContext, userKey, ct);

                return UserDeleteResult.Success(UserDeleteMode.Soft);
            }

            // Hard delete
            if (user.IsDeleted == false)
            {
                // Optional safety: require soft-delete first
                await _userLifecycleStore.MarkDeletedAsync(tenantId, userKey, _clock.UtcNow, ct);
            }
            
            await _sessionService.RevokeAllAsync(authContext, userKey, ct);
            await _credentials.DeleteAllAsync(tenantId, userKey, ct);
            await _profiles.DeleteAsync(tenantId, userKey, mode, ct);
            await _userLifecycleStore.DeleteAsync(tenantId, userKey, ct);

            return UserDeleteResult.Success(UserDeleteMode.Hard);
        }




    }
}
