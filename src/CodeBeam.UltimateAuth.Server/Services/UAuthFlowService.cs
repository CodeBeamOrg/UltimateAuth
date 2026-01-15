using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstactions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Server.Services
{
    internal sealed class UAuthFlowService<TUserId> : IUAuthFlowService<TUserId>
    {
        private readonly IAuthFlowContextAccessor _authFlow;
        private readonly IUAuthUserService<TUserId> _users;
        private readonly ISessionOrchestrator _orchestrator;
        private readonly ISessionQueryService _queries;
        private readonly ITokenIssuer _tokens;
        private readonly IUserIdConverterResolver _userIdConverterResolver;
        private readonly IRefreshTokenValidator _tokenValidator;

        public UAuthFlowService(
            IAuthFlowContextAccessor authFlow,
            IUAuthUserService<TUserId> users,
            ISessionOrchestrator orchestrator,
            ISessionQueryService queries,
            ITokenIssuer tokens,
            IUserIdConverterResolver userIdConverterResolver,
            IRefreshTokenValidator tokenValidator)
        {
            _authFlow = authFlow;
            _users = users;
            _orchestrator = orchestrator;
            _queries = queries;
            _tokens = tokens;
            _userIdConverterResolver = userIdConverterResolver;
            _tokenValidator = tokenValidator;
        }

        public Task<MfaChallengeResult> BeginMfaAsync(BeginMfaRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<LoginResult> CompleteMfaAsync(CompleteMfaRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<LoginResult> ExternalLoginAsync(ExternalLoginRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public async Task<LoginResult> LoginAsync(AuthFlowContext flow, LoginRequest request, CancellationToken ct = default)
        {
            var now = request.At ?? DateTimeOffset.UtcNow;

            var auth = await _users.AuthenticateAsync(request.TenantId, request.Identifier, request.Secret, ct);

            if (!auth.Succeeded)
                return LoginResult.Failed();

            var converter = _userIdConverterResolver.GetConverter<TUserId>();
            var userKey = UserKey.FromString(converter.ToString(auth.UserId!));
            var sessionContext = new AuthenticatedSessionContext
            {
                TenantId = request.TenantId,
                UserKey = userKey,
                Now = now,
                Device = request.Device,
                Claims = auth.Claims,
                ChainId = request.ChainId,
                Metadata = SessionMetadata.Empty // TODO: Check all SessionMetadata.Empty statements
            };

            var authContext = flow.ToAuthContext(now);
            var issuedSession = await _orchestrator.ExecuteAsync(authContext, new CreateLoginSessionCommand<TUserId>(sessionContext), ct);

            bool shouldIssueTokens = request.RequestTokens;

            AuthTokens? tokens = null;

            if (shouldIssueTokens)
            {
                var tokenContext = new TokenIssuanceContext
                {
                    TenantId = request.TenantId,
                    UserKey = userKey,
                    SessionId = issuedSession.Session.SessionId,
                    ChainId = request.ChainId,
                    Claims = auth.Claims.AsDictionary()
                };

                var access = await _tokens.IssueAccessTokenAsync(flow, tokenContext, ct);
                var refresh = await _tokens.IssueRefreshTokenAsync(flow, tokenContext, RefreshTokenPersistence.Persist, ct);

                tokens = new AuthTokens { AccessToken = access, RefreshToken = refresh };
            }

            return LoginResult.Success(issuedSession.Session.SessionId, tokens);
        }

        public async Task<LoginResult> LoginAsync(AuthFlowContext flow, AuthExecutionContext execution, LoginRequest request, CancellationToken ct = default)
        {
            var now = request.At ?? DateTimeOffset.UtcNow;

            var auth = await _users.AuthenticateAsync(request.TenantId, request.Identifier, request.Secret, ct);

            if (!auth.Succeeded)
                return LoginResult.Failed();

            var converter = _userIdConverterResolver.GetConverter<TUserId>();
            var userKey = UserKey.FromString(converter.ToString(auth.UserId!));
            var sessionContext = new AuthenticatedSessionContext
            {
                TenantId = request.TenantId,
                UserKey = userKey,
                Now = now,
                Device = request.Device,
                Claims = auth.Claims,
                ChainId = request.ChainId,
                Metadata = SessionMetadata.Empty
            };

            var authContext = flow.ToAuthContext(now);
            var issuedSession = await _orchestrator.ExecuteAsync(authContext, new CreateLoginSessionCommand<TUserId>(sessionContext), ct);

            bool shouldIssueTokens = request.RequestTokens;

            AuthTokens? tokens = null;

            if (shouldIssueTokens)
            {
                var tokenContext = new TokenIssuanceContext
                {
                    TenantId = request.TenantId,
                    UserKey = userKey,
                    SessionId = issuedSession.Session.SessionId,
                    ChainId = request.ChainId,
                    Claims = auth.Claims.AsDictionary()
                };


                var effectiveFlow = execution.EffectiveClientProfile is null
                    ? flow
                    : flow.WithClientProfile((UAuthClientProfile)execution.EffectiveClientProfile);

                var access = await _tokens.IssueAccessTokenAsync(effectiveFlow, tokenContext, ct);

                var refresh = await _tokens.IssueRefreshTokenAsync(effectiveFlow, tokenContext, RefreshTokenPersistence.Persist, ct);

                tokens = new AuthTokens
                {
                    AccessToken = access,
                    RefreshToken = refresh
                };
            }

            return LoginResult.Success(issuedSession.Session.SessionId, tokens);
        }

        public Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
        {
            var authFlow = _authFlow.Current;
            var now = request.At ?? DateTimeOffset.UtcNow;
            var authContext = authFlow.ToAuthContext(now);

            return _orchestrator.ExecuteAsync(authContext, new RevokeSessionCommand(request.TenantId, request.SessionId), ct);
        }

        public async Task LogoutAllAsync(LogoutAllRequest request, CancellationToken ct = default)
        {
            var authFlow = _authFlow.Current;
            var now = request.At ?? DateTimeOffset.UtcNow;

            if (authFlow.Session is not SessionSecurityContext session)
                throw new InvalidOperationException("LogoutAll requires an active session.");

            var authContext = authFlow.ToAuthContext(now);
            SessionChainId? exceptChainId = null;

            if (request.ExceptCurrent)
            {
                exceptChainId = session.ChainId;

                if (exceptChainId is null)
                    throw new InvalidOperationException("Current session chain could not be resolved.");
            }

            if (authFlow.UserKey is UserKey uaKey)
            {
                var command = new RevokeAllChainsCommand(uaKey, exceptChainId);
                await _orchestrator.ExecuteAsync(authContext, command, ct);
            }
            
        }

        public Task<ReauthResult> ReauthenticateAsync(ReauthRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

    }
}
