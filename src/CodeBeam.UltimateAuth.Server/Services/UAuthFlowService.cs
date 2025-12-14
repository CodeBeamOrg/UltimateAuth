using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Models;

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

        public async Task<LoginResult> LoginAsync(
            LoginRequest request,
            CancellationToken ct = default)
        {
            // burayı birazdan dolduracağız
            throw new NotImplementedException();
        }

        public Task LogoutAllAsync(LogoutAllRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
        {
            throw new NotImplementedException();
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
