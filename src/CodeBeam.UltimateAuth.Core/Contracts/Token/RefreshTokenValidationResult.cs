using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record RefreshTokenValidationResult<TUserId>
    {
        public bool IsValid { get; init; }

        public TUserId? UserId { get; init; }

        public AuthSessionId? SessionId { get; init; }

        private RefreshTokenValidationResult() { }

        public static RefreshTokenValidationResult<TUserId> Invalid()
            => new()
            {
                IsValid = false
            };

        public static RefreshTokenValidationResult<TUserId> Valid(
            TUserId userId,
            AuthSessionId sessionId)
            => new()
            {
                IsValid = true,
                UserId = userId,
                SessionId = sessionId
            };
    }
}
