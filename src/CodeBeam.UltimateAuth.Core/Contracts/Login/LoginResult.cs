using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record LoginResult
    {
        public LoginStatus Status { get; init; }
        public AuthSessionId? SessionId { get; init; }
        public AccessToken? AccessToken { get; init; }
        public RefreshToken? RefreshToken { get; init; }
        public LoginContinuation? Continuation { get; init; }

        // Helpers
        public bool IsSuccess => Status == LoginStatus.Success;
        public bool RequiresContinuation => Continuation is not null;
        public bool RequiresMfa => Continuation?.Type == LoginContinuationType.Mfa;
        public bool RequiresPkce => Continuation?.Type == LoginContinuationType.Pkce;

        public static LoginResult Failed() => new() { Status = LoginStatus.Failed };

        public static LoginResult Success(AuthSessionId sessionId, AuthTokens? tokens = null)
            => new()
            {
                Status = LoginStatus.Success,
                SessionId = sessionId,
                AccessToken = tokens?.AccessToken,
                RefreshToken = tokens?.RefreshToken
            };

        public static LoginResult Continue(LoginContinuation continuation)
            => new()
            {
                Status = LoginStatus.RequiresContinuation,
                Continuation = continuation
            };
    }
}
