using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Handles authentication flows such as login,
    /// logout, session refresh and reauthentication.
    /// </summary>
    public interface IUAuthFlowService<TUserId>
    {
        Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken ct = default);

        Task<LoginResult> ExternalLoginAsync(ExternalLoginRequest request, CancellationToken ct = default);

        Task<MfaChallengeResult> BeginMfaAsync(BeginMfaRequest request, CancellationToken ct = default);

        Task<LoginResult> CompleteMfaAsync(CompleteMfaRequest request, CancellationToken ct = default);

        Task LogoutAsync(LogoutRequest request, CancellationToken ct = default);

        Task LogoutAllAsync(LogoutAllRequest request, CancellationToken ct = default);

        Task<SessionRefreshResult> RefreshSessionAsync(SessionRefreshRequest request, CancellationToken ct = default);

        Task<ReauthResult> ReauthenticateAsync(ReauthRequest request, CancellationToken ct = default);

        Task<PkceChallengeResult> CreatePkceChallengeAsync(PkceCreateRequest request, CancellationToken ct = default);

        Task<PkceVerificationResult> VerifyPkceAsync(PkceVerifyRequest request, CancellationToken ct = default);

        Task ConsumePkceAsync(PkceConsumeRequest request, CancellationToken ct = default);
    }
}
