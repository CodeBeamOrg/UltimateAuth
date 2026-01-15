using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

/// <summary>
/// Low-level persistence abstraction for refresh tokens.
/// NO validation logic. NO business rules.
/// </summary>
public interface IRefreshTokenStore
{
    Task StoreAsync(string? tenantId, StoredRefreshToken token, CancellationToken ct = default);

    Task<StoredRefreshToken?> FindByHashAsync(string? tenantId, string tokenHash, CancellationToken ct = default);

    Task RevokeAsync(string? tenantId, string tokenHash, DateTimeOffset revokedAt, string? replacedByTokenHash = null, CancellationToken ct = default);

    Task RevokeBySessionAsync(string? tenantId, AuthSessionId sessionId, DateTimeOffset revokedAt, CancellationToken ct = default);

    Task RevokeByChainAsync(string? tenantId, SessionChainId chainId, DateTimeOffset revokedAt, CancellationToken ct = default);

    Task RevokeAllForUserAsync(string? tenantId, UserKey userKey, DateTimeOffset revokedAt, CancellationToken ct = default);
}
