using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Provides persistence and validation support for issued tokens,
    /// including refresh tokens and optional access token identifiers (jti).
    /// </summary>
    public interface ITokenStore<TUserId>
    {
        /// <summary>
        /// Persists a refresh token hash associated with a session.
        /// </summary>
        Task StoreRefreshTokenAsync(
            string? tenantId,
            TUserId userId,
            AuthSessionId sessionId,
            string refreshTokenHash,
            DateTimeOffset expiresAt);

        /// <summary>
        /// Validates a provided refresh token against the stored hash.
        /// Returns true if valid and not expired or revoked.
        /// </summary>
        Task<RefreshTokenValidationResult<TUserId>> ValidateRefreshTokenAsync(
            string? tenantId,
            string providedRefreshToken,
            DateTimeOffset now);


        /// <summary>
        /// Revokes the refresh token associated with the specified session.
        /// </summary>
        Task RevokeRefreshTokenAsync(
            string? tenantId,
            AuthSessionId sessionId,
            DateTimeOffset at);

        /// <summary>
        /// Revokes all refresh tokens belonging to the user.
        /// </summary>
        Task RevokeAllRefreshTokensAsync(
            string? tenantId,
            TUserId userId,
            DateTimeOffset at);

        // ------------------------------------------------------------
        // ACCESS TOKEN IDENTIFIERS (OPTIONAL)
        // ------------------------------------------------------------

        /// <summary>
        /// Stores a JWT ID (jti) for replay detection or revocation.
        /// Implementations may ignore this if not supported.
        /// </summary>
        Task StoreTokenIdAsync(
            string? tenantId,
            string jti,
            DateTimeOffset expiresAt);

        /// <summary>
        /// Determines whether the specified token identifier has been revoked.
        /// </summary>
        Task<bool> IsTokenIdRevokedAsync(
            string? tenantId,
            string jti);

        /// <summary>
        /// Revokes a token identifier, preventing further usage.
        /// </summary>
        Task RevokeTokenIdAsync(
            string? tenantId,
            string jti,
            DateTimeOffset at);
    }
}
