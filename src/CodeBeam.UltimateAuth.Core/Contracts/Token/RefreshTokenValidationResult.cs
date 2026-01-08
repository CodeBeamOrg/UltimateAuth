using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record RefreshTokenValidationResult<TUserId>
    {
        public bool IsValid { get; init; }

        public bool IsReuseDetected { get; init; }
        public string? TenantId { get; init; }

        public TUserId? UserId { get; init; }

        public AuthSessionId? SessionId { get; init; }

        public ChainId? ChainId { get; init; }

        public DateTimeOffset? ExpiresAt { get; init; }


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

        public static RefreshTokenValidationResult<TUserId> ReuseDetected(
        string? tenantId = null,
        AuthSessionId? sessionId = null,
        ChainId? chainId = null,
        TUserId? userId = default)
        => new()
        {
            IsValid = false,
            IsReuseDetected = true,
            TenantId = tenantId,
            SessionId = sessionId,
            ChainId = chainId,
            UserId = userId
        };

        public static RefreshTokenValidationResult<TUserId> Valid(
        string? tenantId,
        TUserId userId,
        AuthSessionId sessionId,
        ChainId? chainId = null)
        => new()
        {
            IsValid = true,
            IsReuseDetected = false,
            TenantId = tenantId,
            UserId = userId,
            SessionId = sessionId,
            ChainId = chainId
        };
    }
}
