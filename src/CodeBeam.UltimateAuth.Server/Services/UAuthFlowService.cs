using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Services
{
    internal sealed class UAuthFlowService<TUserId> : IUAuthFlowService
    {
        private readonly IUAuthUserService<TUserId> _users;
        private readonly ISessionOrchestrator<TUserId> _orchestrator;
        private readonly ISessionQueryService<TUserId> _queries;
        private readonly ITokenIssuer _tokens;
        private readonly IRefreshTokenResolver<TUserId> _refreshTokens;

        public UAuthFlowService(
            IUAuthUserService<TUserId> users,
            ISessionOrchestrator<TUserId> orchestrator,
            ISessionQueryService<TUserId> queries,
            ITokenIssuer tokens,
            IRefreshTokenResolver<TUserId> refreshTokens)
        {
            _users = users;
            _orchestrator = orchestrator;
            _queries = queries;
            _tokens = tokens;
            _refreshTokens = refreshTokens;
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

        public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            var now = request.At ?? DateTimeOffset.UtcNow;
            var device = request.DeviceInfo ?? DeviceInfo.Unknown;

            // 1️⃣ Authenticate user (NO session yet)
            var auth = await _users.AuthenticateAsync(request.TenantId, request.Identifier, request.Secret, ct);

            if (!auth.Succeeded)
                return LoginResult.Failed();

            // 2️⃣ Create authenticated context
            var sessionContext = new AuthenticatedSessionContext<TUserId>
            {
                TenantId = request.TenantId,
                UserId = auth.UserId!,
                Now = now,
                DeviceInfo = device,
                Claims = auth.Claims,
                ChainId = request.ChainId
            };

            var authContext = AuthContext.ForAuthenticatedUser(
                request.TenantId,
                AuthOperation.Login,
                now,
                DeviceContext.From(device));

            // 3️⃣ Issue session THROUGH orchestrator
            var issuedSession = await _orchestrator.ExecuteAsync(
                authContext,
                new CreateLoginSessionCommand<TUserId>(sessionContext),
                ct);

            // 4️⃣ Optional tokens
            AuthTokens? tokens = null;

            if (request.RequestTokens)
            {
                var access = await _tokens.IssueAccessTokenAsync(
                    new TokenIssuanceContext
                    {
                        TenantId = request.TenantId,
                        UserId = auth.UserId!.ToString()!,
                        SessionId = issuedSession.Session.SessionId
                    },
                    ct);

                var refresh = await _tokens.IssueRefreshTokenAsync(
                    new TokenIssuanceContext
                    {
                        TenantId = request.TenantId,
                        UserId = auth.UserId!.ToString()!,
                        SessionId = issuedSession.Session.SessionId
                    },
                    ct);

                tokens = new AuthTokens { AccessToken = access, RefreshToken = refresh };
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

            var authContext = AuthContext.System(
                request.TenantId,
                AuthOperation.Revoke,
                now);

            await _orchestrator.ExecuteAsync(
                authContext,
                new RevokeAllChainsCommand<TUserId>(
                    userId,
                    exceptChainId),
                ct);
        }

        public Task<ReauthResult> ReauthenticateAsync(ReauthRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public async Task<SessionRefreshResult> RefreshSessionAsync(SessionRefreshRequest request, CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow;
            var resolved = await _refreshTokens.ResolveAsync(request.TenantId, request.RefreshToken, now, ct);

            if (resolved is null)
                return SessionRefreshResult.Invalid();

            if (!resolved.IsValid)
            {
                // TODO: Add reuse detection handling here
                //if (resolved.IsReuseDetected)
                //{
                //    await _sessions.RevokeChainAsync(
                //        tenantId,
                //        resolved.Chain!.ChainId,
                //        now);
                //}

                //return SessionRefreshResult.ReauthRequired();
            }

            var session = resolved.Session;

            var rotationContext = new SessionRotationContext<TUserId>
            {
                TenantId = request.TenantId,
                CurrentSessionId = session.SessionId,
                UserId = session.UserId,
                Now = now
            };

            var authContext = AuthContext.ForAuthenticatedUser(request.TenantId, AuthOperation.Refresh, now, DeviceContext.From(session.Device));

            var issuedSession = await _orchestrator.ExecuteAsync(authContext, new RotateSessionCommand<TUserId>(rotationContext), ct);

            var tokenContext = new TokenIssuanceContext
            {
                TenantId = request.TenantId,
                UserId = session.UserId!.ToString()!,
                SessionId = issuedSession.Session.SessionId
            };

            var accessToken = await _tokens.IssueAccessTokenAsync(tokenContext, ct);
            var refreshToken = await _tokens.IssueRefreshTokenAsync(tokenContext, ct);

            return SessionRefreshResult.Success(accessToken, refreshToken);
        }

        public Task<PkceVerificationResult> VerifyPkceAsync(PkceVerifyRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }

}
