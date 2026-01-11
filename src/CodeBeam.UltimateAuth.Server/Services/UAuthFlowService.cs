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
        private readonly IUAuthUserService<TUserId> _users;
        private readonly ISessionOrchestrator<TUserId> _orchestrator;
        private readonly ISessionQueryService<TUserId> _queries;
        private readonly ITokenIssuer<TUserId> _tokens;
        private readonly IRefreshTokenValidator<TUserId> _tokenValidator;

        public UAuthFlowService(
            IUAuthUserService<TUserId> users,
            ISessionOrchestrator<TUserId> orchestrator,
            ISessionQueryService<TUserId> queries,
            ITokenIssuer<TUserId> tokens,
            IRefreshTokenValidator<TUserId> tokenValidator)
        {
            _users = users;
            _orchestrator = orchestrator;
            _queries = queries;
            _tokens = tokens;
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

        public Task ConsumePkceAsync(PkceConsumeRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<PkceChallengeResult> CreatePkceChallengeAsync(PkceCreateRequest request, CancellationToken ct = default)
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
            var device = request.DeviceInfo ?? DeviceInfo.Unknown;

            var auth = await _users.AuthenticateAsync(request.TenantId, request.Identifier, request.Secret, ct);

            if (!auth.Succeeded)
                return LoginResult.Failed();

            var sessionContext = new AuthenticatedSessionContext<TUserId>
            {
                TenantId = request.TenantId,
                UserId = auth.UserId!,
                Now = now,
                DeviceInfo = device,
                Claims = auth.Claims,
                ChainId = request.ChainId
            };

            var authContext = AuthContext.ForAuthenticatedUser(request.TenantId, AuthOperation.Login, now, DeviceContext.From(device));
            var issuedSession = await _orchestrator.ExecuteAsync(authContext, new CreateLoginSessionCommand<TUserId>(sessionContext), ct);

            bool shouldIssueTokens = request.RequestTokens;

            AuthTokens? tokens = null;

            if (shouldIssueTokens)
            {
                var tokenContext = new TokenIssuanceContext
                {
                    TenantId = request.TenantId,
                    UserId = auth.UserId!.ToString()!,
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
            var device = request.DeviceInfo ?? DeviceInfo.Unknown;

            var auth = await _users.AuthenticateAsync(request.TenantId, request.Identifier, request.Secret, ct);

            if (!auth.Succeeded)
                return LoginResult.Failed();

            var sessionContext = new AuthenticatedSessionContext<TUserId>
            {
                TenantId = request.TenantId,
                UserId = auth.UserId!,
                Now = now,
                DeviceInfo = device,
                Claims = auth.Claims,
                ChainId = request.ChainId
            };

            var authContext = AuthContext.ForAuthenticatedUser(request.TenantId, AuthOperation.Login, now, DeviceContext.From(device));
            var issuedSession = await _orchestrator.ExecuteAsync(authContext, new CreateLoginSessionCommand<TUserId>(sessionContext), ct);

            bool shouldIssueTokens = request.RequestTokens;

            AuthTokens? tokens = null;

            if (shouldIssueTokens)
            {
                var tokenContext = new TokenIssuanceContext
                {
                    TenantId = request.TenantId,
                    UserId = auth.UserId!.ToString()!,
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
            var now = request.At ?? DateTimeOffset.UtcNow;
            var authContext = AuthContext.System(request.TenantId, AuthOperation.Revoke,now);

            return _orchestrator.ExecuteAsync(authContext, new RevokeSessionCommand<TUserId>(request.TenantId, request.SessionId), ct);
        }

        public async Task LogoutAllAsync(LogoutAllRequest request, CancellationToken ct = default)
        {
            var now = request.At ?? DateTimeOffset.UtcNow;

            if (request.CurrentSessionId is null)
                throw new InvalidOperationException("CurrentSessionId must be provided for logout-all operation.");

            var currentSessionId = request.CurrentSessionId.Value;

            var validation = await _queries.ValidateSessionAsync(
                new SessionValidationContext
                {
                    TenantId = request.TenantId,
                    SessionId = currentSessionId,
                    Now = now
                },
                ct);

            if (!validation.IsValid || validation.Session is null)
                throw new InvalidOperationException("Current session is not valid.");

            var userId = validation.Session.UserId;

            ChainId? exceptChainId = null;

            if (request.ExceptCurrent)
            {
                exceptChainId = await _queries.ResolveChainIdAsync(
                    request.TenantId,
                    currentSessionId,
                    ct);

                if (exceptChainId is null)
                    throw new InvalidOperationException("Current session chain could not be resolved.");
            }

            var authContext = AuthContext.System(request.TenantId, AuthOperation.Revoke, now);
            await _orchestrator.ExecuteAsync(authContext, new RevokeAllChainsCommand<TUserId>(userId, exceptChainId), ct);
        }

        public Task<ReauthResult> ReauthenticateAsync(ReauthRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<PkceVerificationResult> VerifyPkceAsync(PkceVerifyRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }

}
