using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

/// <summary>
/// Low-level persistence abstraction for refresh tokens.
/// NO validation logic. NO business rules.
/// </summary>
public interface IRefreshTokenStore<TUserId>
{
    Task StoreAsync(string? tenantId,
        StoredRefreshToken<TUserId> token,
        CancellationToken ct = default);

    Task<StoredRefreshToken<TUserId>?> FindByHashAsync(string? tenantId,
        string tokenHash,
        CancellationToken ct = default);

    Task RevokeAsync(string? tenantId,
        string tokenHash,
        DateTimeOffset revokedAt,
        CancellationToken ct = default);

    Task RevokeBySessionAsync(string? tenantId,
        AuthSessionId sessionId,
        DateTimeOffset revokedAt,
        CancellationToken ct = default);

    Task RevokeByChainAsync(string? tenantId,
        ChainId chainId,
        DateTimeOffset revokedAt,
        CancellationToken ct = default);

    Task RevokeAllForUserAsync(string? tenantId,
        TUserId userId,
        DateTimeOffset revokedAt,
        CancellationToken ct = default);
}
