using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record RefreshTokenValidationResult
{
    public RefreshTokenValidationState State { get; init; }

    public bool IsValid => State == RefreshTokenValidationState.Valid;

    public string? TokenHash { get; init; }

    public TenantKey Tenant { get; init; }

    public UserKey? UserKey { get; init; }

    public AuthSessionId? SessionId { get; init; }

    public SessionChainId? SessionChainId { get; init; }

    public RefreshChainId? RefreshChainId { get; init; }

    public DeviceId? DeviceId { get; init; }

    public DateTimeOffset? AuthenticatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public DateTimeOffset? ConsumedAt { get; init; }

    public string? ReplacedByTokenHash { get; init; }

    private RefreshTokenValidationResult()
    {
    }

    public static RefreshTokenValidationResult Invalid()
        => new()
        {
            State = RefreshTokenValidationState.Invalid
        };

    public static RefreshTokenValidationResult NotFound()
        => new()
        {
            State = RefreshTokenValidationState.NotFound
        };

    public static RefreshTokenValidationResult Expired(RefreshToken token)
        => FromToken(token, RefreshTokenValidationState.Expired);

    public static RefreshTokenValidationResult Consumed(RefreshToken token)
        => FromToken(token, RefreshTokenValidationState.Consumed);

    public static RefreshTokenValidationResult Valid(RefreshToken token, string tokenHash)
        => new()
        {
            State = RefreshTokenValidationState.Valid,

            Tenant = token.Tenant,
            UserKey = token.UserKey,
            SessionId = token.SessionId,
            SessionChainId = token.SessionChainId,
            RefreshChainId = token.RefreshChainId,
            DeviceId = token.DeviceId,

            TokenHash = tokenHash,
            AuthenticatedAt = token.AuthenticatedAt,
            ExpiresAt = token.ExpiresAt,
            ReplacedByTokenHash = token.ReplacedByTokenHash
        };

    private static RefreshTokenValidationResult FromToken(RefreshToken token, RefreshTokenValidationState state)
        => new()
        {
            State = state,

            Tenant = token.Tenant,
            UserKey = token.UserKey,
            SessionId = token.SessionId,
            SessionChainId = token.SessionChainId,
            RefreshChainId = token.RefreshChainId,
            DeviceId = token.DeviceId,

            TokenHash = token.TokenHash,
            AuthenticatedAt = token.AuthenticatedAt,
            ExpiresAt = token.ExpiresAt,
            ReplacedByTokenHash = token.ReplacedByTokenHash,

            ConsumedAt = token.RevokedAt
        };
}