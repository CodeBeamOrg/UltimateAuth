using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Services
{
    internal sealed class UAuthFlowService<TUserId> : IUAuthFlowService
    {
        private readonly IUAuthUserService<TUserId> _users;
        private readonly IUAuthSessionService<TUserId> _sessions;
        private readonly IUAuthTokenService<TUserId> _tokens;

        public UAuthFlowService(
            IUAuthUserService<TUserId> users,
            IUAuthSessionService<TUserId> sessions,
            IUAuthTokenService<TUserId> tokens)
        {
            _users = users;
            _sessions = sessions;
            _tokens = tokens;
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
            var now = request.At ?? DateTime.UtcNow;
            var device = request.DeviceInfo ?? DeviceInfo.Unknown;

            var authResult = await _users.AuthenticateAsync(request.TenantId, request.Identifier, request.Secret, ct);

            if (!authResult.Succeeded)
            {
                return LoginResult.Failed();
            }

            var sessionResult = await _sessions.IssueSessionAfterAuthenticationAsync(request.TenantId,
                new AuthenticatedSessionContext<TUserId>
                {
                    TenantId = request.TenantId,
                    UserId = authResult.UserId!,
                    Now = now,
                    DeviceInfo = device,
                    Claims = authResult.Claims,
                    ChainId = request.ChainId
                });

            AuthTokens? tokens = null;

            if (request.RequestTokens)
            {
                tokens = await _tokens.CreateTokensAsync(
                    new TokenIssueContext<TUserId>
                    {
                        TenantId = request.TenantId,
                        Session = sessionResult.Session,
                        Now = now
                    },
                    ct);
            }

            return LoginResult.Success(sessionResult.Session.SessionId, tokens);
        }

        public async Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
        {
            var at = request.At ?? DateTime.UtcNow;
            await _sessions.RevokeSessionAsync(request.TenantId, request.SessionId, at);
        }

        public async Task LogoutAllAsync(LogoutAllRequest request, CancellationToken ct = default)
        {
            var at = request.At ?? DateTime.UtcNow;

            if (request.CurrentSessionId is null)
                throw new InvalidOperationException(
                    "CurrentSessionId must be provided for logout-all operation.");

            var currentSessionId = request.CurrentSessionId.Value;

            var validation = await _sessions.ValidateSessionAsync(
                request.TenantId,
                currentSessionId,
                at);

            if (validation.IsValid ||
                validation.Session is null)
                throw new InvalidOperationException("Current session is not valid.");

            var userId = validation.Session.UserId;

            ChainId? currentChainId = null;

            if (request.ExceptCurrent)
            {
                if (request.CurrentSessionId is null)
                    throw new InvalidOperationException("CurrentSessionId must be provided when ExceptCurrent is true.");

                currentChainId = await _sessions.ResolveChainIdAsync(
                    request.TenantId,
                    currentSessionId);

                if (currentChainId is null)
                    throw new InvalidOperationException("Current session chain could not be resolved.");
            }

            await _sessions.RevokeAllChainsAsync(request.TenantId, userId, exceptChainId: currentChainId, at);
        }

        public Task<ReauthResult> ReauthenticateAsync(ReauthRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<SessionRefreshResult> RefreshSessionAsync(SessionRefreshRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<PkceVerificationResult> VerifyPkceAsync(PkceVerifyRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }

}
