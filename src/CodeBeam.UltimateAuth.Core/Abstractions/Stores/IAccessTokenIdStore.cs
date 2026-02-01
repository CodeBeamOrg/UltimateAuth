namespace CodeBeam.UltimateAuth.Core.Abstractions;

/// <summary>
/// Optional persistence for access token identifiers (jti).
/// Used for revocation and replay protection.
/// </summary>
public interface IAccessTokenIdStore
{
    Task StoreAsync(string? tenantId, string jti, DateTimeOffset expiresAt, CancellationToken ct = default);

    Task<bool> IsRevokedAsync(string? tenantId, string jti, CancellationToken ct = default);

    Task RevokeAsync(string? tenantId, string jti, DateTimeOffset revokedAt, CancellationToken ct = default);
}
