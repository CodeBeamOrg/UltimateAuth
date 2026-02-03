using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record TokenValidationResult<TUserId>
{
    public bool IsValid { get; init; }
    public TokenType Type { get; init; }
    public TenantKey? Tenant { get; init; }
    public TUserId? UserId { get; init; }
    public AuthSessionId? SessionId { get; init; }
    public IReadOnlyCollection<Claim> Claims { get; init; } = Array.Empty<Claim>();
    public TokenInvalidReason? InvalidReason { get; init; }
    public DateTimeOffset? ExpiresAt { get; set; }

    private TokenValidationResult(
        bool isValid,
        TokenType type,
        TenantKey? tenant,
        TUserId? userId,
        AuthSessionId? sessionId,
        IReadOnlyCollection<Claim>? claims,
        TokenInvalidReason? invalidReason,
        DateTimeOffset? expiresAt
        )
    {
        IsValid = isValid;
        Tenant = tenant;
        UserId = userId;
        SessionId = sessionId;
        Claims = claims ?? Array.Empty<Claim>();
        InvalidReason = invalidReason;
        ExpiresAt = expiresAt;
    }

    public static TokenValidationResult<TUserId> Valid(
        TokenType type,
        TenantKey tenant,
        TUserId userId,
        AuthSessionId? sessionId,
        IReadOnlyCollection<Claim> claims,
        DateTimeOffset? expiresAt)
        => new(
            isValid: true,
            type,
            tenant,
            userId,
            sessionId,
            claims,
            invalidReason: null,
            expiresAt
            );

    public static TokenValidationResult<TUserId> Invalid(TokenType type, TokenInvalidReason reason)
        => new(
            isValid: false,
            type: type,
            tenant: null,
            userId: default,
            sessionId: null,
            claims: null,
            invalidReason: reason,
            expiresAt: null
            );
}
