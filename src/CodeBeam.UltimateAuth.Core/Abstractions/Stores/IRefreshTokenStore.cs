using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

/// <summary>
/// Low-level persistence abstraction for refresh tokens.
/// NO validation logic. NO business rules.
/// </summary>
public interface IRefreshTokenStore
{
    Task StoreAsync(TenantKey tenant, StoredRefreshToken token, CancellationToken ct = default);

    Task<StoredRefreshToken?> FindByHashAsync(TenantKey tenant, string tokenHash, CancellationToken ct = default);

    Task RevokeAsync(TenantKey tenant, string tokenHash, DateTimeOffset revokedAt, string? replacedByTokenHash = null, CancellationToken ct = default);

    Task RevokeBySessionAsync(TenantKey tenant, AuthSessionId sessionId, DateTimeOffset revokedAt, CancellationToken ct = default);

    Task RevokeByChainAsync(TenantKey tenant, SessionChainId chainId, DateTimeOffset revokedAt, CancellationToken ct = default);

    Task RevokeAllForUserAsync(TenantKey tenant, UserKey userKey, DateTimeOffset revokedAt, CancellationToken ct = default);
}
