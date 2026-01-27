using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.Credentials.Reference.Internal;
using CodeBeam.UltimateAuth.Server.Defaults;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference.Domain;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    internal sealed class DefaultUserLifecycleService : IUserLifecycleService
    {
        private readonly IAccessOrchestrator _accessOrchestrator;
        private readonly IUserStore<UserKey> _users;
        private readonly IUserProfileStore _profiles;
        private readonly IUserLifecycleStore _userLifecycleStore;
        private readonly IUserCredentialsService _credentials;
        private readonly IUserCredentialsInternalService _credentialsInternal;
        private readonly IUserIdentifierService _identifierService;
        private readonly ISessionService _sessionService;
        private readonly IAuthContextFactory _authContextFactory;
        private readonly IClock _clock;

        public DefaultUserLifecycleService(
            IAccessOrchestrator accessOrchestrator,
            IUserStore<UserKey> users,
            IUserProfileStore profiles,
            IUserLifecycleStore userLifecycleStore,
            IUserCredentialsService credentials,
            IUserCredentialsInternalService credentialsInternal,
            IUserIdentifierService identifierService,
            ISessionService sessionService,
            IAuthContextFactory authContextFactory,
            IClock clock)
        {
            _accessOrchestrator = accessOrchestrator;
            _users = users;
            _profiles = profiles;
            _userLifecycleStore = userLifecycleStore;
            _credentials = credentials;
            _credentialsInternal = credentialsInternal;
            _identifierService = identifierService;
            _sessionService = sessionService;
            _authContextFactory = authContextFactory;
            _clock = clock;
        }

        public async Task<UserCreateResult> CreateAsync(AccessContext context, CreateUserRequest request, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request.Identifier))
                return UserCreateResult.Failed("identifier_required");

            var policies = Array.Empty<IAccessPolicy>();
            var userKey = UserKey.New();
            var now = _clock.UtcNow;

            var cmd = new CreateUserCommand(
                async innerCt =>
                {
                    var existing = await _users.FindByLoginAsync(context.ResourceTenantId, request.Identifier, innerCt);

                    if (existing is not null)
                        throw new InvalidOperationException("user_already_exists");

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
                        FirstName = request.Profile?.FirstName,
                        LastName = request.Profile?.LastName,
                    };

                    await _userLifecycleStore.CreateAsync(context.ResourceTenantId, profile, innerCt);
                    await _profiles.CreateAsync(context.ResourceTenantId, profile, innerCt);

                    if (!string.IsNullOrWhiteSpace(request.Password))
                    {
                        await _credentials.AddAsync(
                            new AccessContext
                            {
                                ActorUserKey = context.ActorUserKey,
                                ActorTenantId = context.ActorTenantId,
                                IsAuthenticated = context.IsAuthenticated,

                                Resource = "credentials",
                                ResourceId = userKey.Value,
                                ResourceTenantId = context.ResourceTenantId,

                                Action = UAuthActions.Credentials.Add
                            },
                            new AddCredentialRequest
                            {
                                Type = CredentialType.Password,
                                Secret = request.Password
                            },
                            innerCt);

                    }
                });

            await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
            return UserCreateResult.Success(userKey);
        }

        public async Task<UserStatusChangeResult> ChangeStatusAsync(AccessContext context, ChangeUserStatusRequest request, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var policies = Array.Empty<IAccessPolicy>();
            UserStatus oldStatus = default;

            var cmd = new ChangeUserStatusCommand(
                async innerCt =>
                {
                    var profile = await _profiles.GetAsync(context.ResourceTenantId, request.UserKey, innerCt);

                    if (profile is null)
                        throw new InvalidOperationException("user_not_found");

                    if (profile.Status == request.NewStatus)
                        return;

                    oldStatus = profile.Status;
                    //await _profiles.SetStatusAsync(context.ResourceTenantId, request.UserKey, request.NewStatus, innerCt);
                });

            await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
            return UserStatusChangeResult.Success(oldStatus, request.NewStatus);
        }

        public async Task<UserDeleteResult> DeleteAsync(AccessContext context, DeleteUserRequest request, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var policies = Array.Empty<IAccessPolicy>();
            
            var cmd = new DeleteUserCommand(
                async innerCt =>
                {
                    var user = await _users.FindByIdAsync(context.ResourceTenantId, request.UserKey, innerCt);

                    if (user is null)
                        throw new InvalidOperationException("user_not_found");

                    var authContext = _authContextFactory.Create();
                    if (request.Mode == DeleteMode.Soft)
                    {
                        if (user.IsDeleted)
                            return;

                        await _userLifecycleStore.DeleteAsync(context.ResourceTenantId, request.UserKey, DeleteMode.Soft, _clock.UtcNow, innerCt);
                        await _sessionService.RevokeAllAsync(authContext, request.UserKey,innerCt);
                        return;
                    }

                    // Hard delete
                    if (!user.IsDeleted)
                    {
                        await _userLifecycleStore.DeleteAsync(context.ResourceTenantId,request.UserKey, DeleteMode.Soft, _clock.UtcNow, innerCt);
                    }

                    await _sessionService.RevokeAllAsync(authContext, request.UserKey, innerCt);
                    await _credentialsInternal.DeleteInternalAsync(context.ResourceTenantId, request.UserKey, innerCt);
                    await _profiles.DeleteAsync(context.ResourceTenantId, request.UserKey, DeleteMode.Hard, innerCt);
                    await _userLifecycleStore.DeleteAsync(context.ResourceTenantId, request.UserKey, DeleteMode.Hard, _clock.UtcNow, innerCt);
                });

            await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
            return UserDeleteResult.Success(request.Mode);
        }

    }
}
