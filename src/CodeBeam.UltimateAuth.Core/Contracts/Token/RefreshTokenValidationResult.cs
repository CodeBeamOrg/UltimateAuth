using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record RefreshTokenValidationResult<TUserId>
    {
        public bool IsValid { get; init; }

        public bool IsReuseDetected { get; init; }

        public TUserId? UserId { get; init; }

        public AuthSessionId? SessionId { get; init; }

        private RefreshTokenValidationResult() { }

        // ----------------------------
        // FACTORIES
        // ----------------------------

        public static RefreshTokenValidationResult<TUserId> Invalid()
            => new()
            {
                IsValid = false,
                IsReuseDetected = false
            };

        public static RefreshTokenValidationResult<TUserId> ReuseDetected()
            => new()
            {
                IsValid = false,
                IsReuseDetected = true
            };

        public static RefreshTokenValidationResult<TUserId> Valid(
            TUserId userId,
            AuthSessionId sessionId)
            => new()
            {
                IsValid = true,
                IsReuseDetected = false,
                UserId = userId,
                SessionId = sessionId
            };
    }
}
