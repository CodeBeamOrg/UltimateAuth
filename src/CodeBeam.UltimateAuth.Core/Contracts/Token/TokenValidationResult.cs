using CodeBeam.UltimateAuth.Core.Domain;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record TokenValidationResult<TUserId>
    {
        public bool IsValid { get; init; }
        public TokenType Type { get; init; }
        public string? TenantId { get; init; }
        public TUserId? UserId { get; init; }
        public AuthSessionId? SessionId { get; init; }
        public IReadOnlyCollection<Claim> Claims { get; init; } = Array.Empty<Claim>();
        public TokenInvalidReason? InvalidReason { get; init; }
        public DateTimeOffset? ExpiresAt { get; set; }

        private TokenValidationResult(
            bool isValid,
            TokenType type,
            string? tenantId,
            TUserId? userId,
            AuthSessionId? sessionId,
            IReadOnlyCollection<Claim>? claims,
            TokenInvalidReason? invalidReason,
            DateTimeOffset? expiresAt
            )
        {
            IsValid = isValid;
            TenantId = tenantId;
            UserId = userId;
            SessionId = sessionId;
            Claims = claims ?? Array.Empty<Claim>();
            InvalidReason = invalidReason;
            ExpiresAt = expiresAt;
        }

        public static TokenValidationResult<TUserId> Valid(
            TokenType type,
            string? tenantId,
            TUserId userId,
            AuthSessionId? sessionId,
            IReadOnlyCollection<Claim> claims,
            DateTimeOffset? expiresAt)
            => new(
                isValid: true,
                type,
                tenantId,
                userId,
                sessionId,
                claims,
                invalidReason: null,
                expiresAt
                );

        public static TokenValidationResult<TUserId> Invalid(TokenType type, TokenInvalidReason reason)
            => new(
                isValid: false,
                type,
                tenantId: null,
                userId: default,
                sessionId: null,
                claims: null,
                invalidReason: reason,
                expiresAt: null
                );
    }
}
