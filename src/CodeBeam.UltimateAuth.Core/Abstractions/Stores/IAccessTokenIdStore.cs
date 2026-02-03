using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

/// <summary>
/// Optional persistence for access token identifiers (jti).
/// Used for revocation and replay protection.
/// </summary>
public interface IAccessTokenIdStore
{
    Task StoreAsync(TenantKey tenant, string jti, DateTimeOffset expiresAt, CancellationToken ct = default);

    Task<bool> IsRevokedAsync(TenantKey tenant, string jti, CancellationToken ct = default);

    Task RevokeAsync(TenantKey tenant, string jti, DateTimeOffset revokedAt, CancellationToken ct = default);
}
