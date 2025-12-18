using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Low-level persistence abstraction for token-related data.
    /// Handles refresh tokens and optional access token identifiers (jti).
    /// </summary>
    public interface ITokenStoreKernel
    {
        Task SaveRefreshTokenAsync(
            string? tenantId,
            StoredRefreshToken token);

        Task<StoredRefreshToken?> GetRefreshTokenAsync(
            string? tenantId,
            string tokenHash);

        Task RevokeRefreshTokenAsync(
            string? tenantId,
            string tokenHash,
            DateTimeOffset at);

        Task RevokeAllRefreshTokensAsync(
            string? tenantId,
            string? userId,
            DateTimeOffset at);

        Task DeleteExpiredRefreshTokensAsync(
            string? tenantId,
            DateTimeOffset now);

        Task StoreTokenIdAsync(
            string? tenantId,
            string jti,
            DateTimeOffset expiresAt);

        Task<bool> IsTokenIdRevokedAsync(
            string? tenantId,
            string jti);

        Task RevokeTokenIdAsync(
            string? tenantId,
            string jti,
            DateTimeOffset at);
    }
}
