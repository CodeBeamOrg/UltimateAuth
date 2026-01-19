using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference.Domain;

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

            var existing = await _users.FindByLoginAsync(tenantId, request.Identifier, ct);
            if (existing is not null)
                return UserCreateResult.Failed("User already exists.");

            var userKey = UserKey.New();
            var now = _clock.UtcNow;

            var profile = new ReferenceUserProfile
            {
                UserKey = userKey,
                Email = request.Identifier,
                DisplayName = request.DisplayName,
                Status = UserStatus.Active,
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now,
                DeletedAt = null,
                FirstName = request?.Profile?.FirstName,
                LastName = request?.Profile?.LastName,
            };

            await _userLifecycleStore.CreateAsync(tenantId, profile, ct);
            await _profiles.CreateAsync(tenantId, profile, ct);


            if (!string.IsNullOrWhiteSpace(request?.Password))
            {
                await _credentials.SetInitialAsync(
                    tenantId,
                    userKey,
                    new SetInitialCredentialRequest
                    {
                        Type = CredentialType.Password,
                        Secret = request.Password
                    });
            }

            return UserCreateResult.Success(userKey);
        }

        public async Task<UserStatusChangeResult> ChangeStatusAsync(string? tenantId, ChangeUserStatusRequest request, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var exists = await _users.ExistsAsync(tenantId, request.UserKey, ct);
            if (!exists)
                return UserStatusChangeResult.NotFound();

            var profile = await _profiles.GetAsync(tenantId, request.UserKey, ct);
            if (profile is null)
                return UserStatusChangeResult.NotFound();

            var oldStatus = profile.Status;
            if (oldStatus == request.NewStatus)
                return UserStatusChangeResult.NoChange(oldStatus);

            await _profiles.SetStatusAsync(tenantId, request.UserKey, request.NewStatus, ct);

            // TODO: Check all status
            if (request.NewStatus is UserStatus.Disabled or UserStatus.Suspended)
            {
                await _credentials.RevokeAllAsync(tenantId, new RevokeAllCredentialsRequest { UserKey = request.UserKey }, ct);
            }

            return UserStatusChangeResult.Success(oldStatus, request.NewStatus);
        }

        public async Task<UserDeleteResult> DeleteAsync(string? tenantId, DeleteUserRequest request, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var user = await _users.FindByIdAsync(tenantId, request.UserKey, ct);
            if (user is null)
                return UserDeleteResult.NotFound();

            var authContext = _authContextFactory.Create();
            if (request.Mode == UserDeleteMode.Soft)
            {
                if (user.IsDeleted)
                    return UserDeleteResult.AlreadyDeleted(UserDeleteMode.Soft);

                await _userLifecycleStore.MarkDeletedAsync(tenantId, request.UserKey, _clock.UtcNow, ct);

                await _credentials.RevokeAllAsync(tenantId, new RevokeAllCredentialsRequest { UserKey = request.UserKey }, ct);
                await _sessionService.RevokeAllAsync(authContext, request.UserKey, ct);

                return UserDeleteResult.Success(UserDeleteMode.Soft);
            }

            // Hard delete
            if (user.IsDeleted == false)
            {
                // Optional safety: require soft-delete first
                await _userLifecycleStore.MarkDeletedAsync(tenantId, request.UserKey, _clock.UtcNow, ct);
            }
            
            await _sessionService.RevokeAllAsync(authContext, request.UserKey, ct);
            await _credentials.DeleteAllAsync(tenantId, request.UserKey, ct);
            await _profiles.DeleteAsync(tenantId, request.UserKey, request.Mode, ct);
            await _userLifecycleStore.DeleteAsync(tenantId, request.UserKey, ct);

            return UserDeleteResult.Success(UserDeleteMode.Hard);
        }




    }
}
