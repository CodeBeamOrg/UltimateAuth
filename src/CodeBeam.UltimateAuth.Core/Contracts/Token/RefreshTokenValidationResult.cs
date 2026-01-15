using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record RefreshTokenValidationResult
    {
        public bool IsValid { get; init; }
        public bool IsReuseDetected { get; init; }

        public string? TokenHash { get; init; }

        public string? TenantId { get; init; }
        public UserKey? UserKey { get; init; }
        public AuthSessionId? SessionId { get; init; }
        public SessionChainId? ChainId { get; init; }

        public DateTimeOffset? ExpiresAt { get; init; }


        private RefreshTokenValidationResult() { }

        public static RefreshTokenValidationResult Invalid()
            => new()
            {
                IsValid = false,
                IsReuseDetected = false
            };

        public static RefreshTokenValidationResult ReuseDetected(
            string? tenantId = null,
            AuthSessionId? sessionId = null,
            string? tokenHash = null,
            SessionChainId? chainId = null,
            UserKey? userKey = default)
        => new()
        {
            IsValid = false,
            IsReuseDetected = true,
            TenantId = tenantId,
            SessionId = sessionId,
            TokenHash = tokenHash,
            ChainId = chainId,
            UserKey = userKey,
        };

        public static RefreshTokenValidationResult Valid(
            string? tenantId,
            UserKey userKey,
            AuthSessionId sessionId,
            string? tokenHash,
            SessionChainId? chainId = null)
        => new()
        {
            IsValid = true,
            IsReuseDetected = false,
            TenantId = tenantId,
            UserKey = userKey,
            SessionId = sessionId,
            ChainId = chainId,
            TokenHash = tokenHash
        };
    }
}
