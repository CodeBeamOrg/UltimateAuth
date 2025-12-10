using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Models;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Provides high-level session lifecycle operations such as creation, refresh, validation, and revocation.
    /// </summary>
    /// <typeparam name="TUserId">The type used to uniquely identify the user.</typeparam>
    public interface ISessionService<TUserId>
    {
        /// <summary>
        /// Creates a new login session for the specified user.
        /// </summary>
        /// <param name="tenantId">
        /// The tenant identifier. Use <c>null</c> for single-tenant applications.
        /// </param>
        /// <param name="userId">The user associated with the session.</param>
        /// <param name="deviceInfo">Information about the device initiating the session.</param>
        /// <param name="metadata">Optional metadata describing the session context.</param>
        /// <param name="now">The current UTC timestamp.</param>
        /// <returns>
        /// A result containing the newly created session, chain, and session root.
        /// </returns>
        Task<SessionResult<TUserId>> CreateLoginSessionAsync(string? tenantId, TUserId userId, DeviceInfo deviceInfo, SessionMetadata? metadata, DateTime now);
        
        /// <summary>
        /// Rotates the specified session and issues a new one while preserving the session chain.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="currentSessionId">The active session identifier to be refreshed.</param>
        /// <param name="now">The current UTC timestamp.</param>
        /// <returns>
        /// A result containing the refreshed session and updated chain.
        /// </returns>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown if the session, its chain, or the user's session root is invalid.
        /// </exception>
        Task<SessionResult<TUserId>> RefreshSessionAsync(string? tenantId, AuthSessionId currentSessionId,DateTime now);

        /// <summary>
        /// Revokes a single session, preventing further use.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="sessionId">The session identifier to revoke.</param>
        /// <param name="at">The UTC timestamp of the revocation.</param>
        Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTime at);

        /// <summary>
        /// Revokes an entire session chain (device-level logout).
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="chainId">The session chain identifier to revoke.</param>
        /// <param name="at">The UTC timestamp of the revocation.</param>
        Task RevokeChainAsync(string? tenantId, ChainId chainId, DateTime at);

        /// <summary>
        /// Revokes the user's session root, invalidating all existing sessions across all chains.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="userId">The user whose root should be revoked.</param>
        /// <param name="at">The UTC timestamp of the revocation.</param>
        Task RevokeRootAsync(string? tenantId, TUserId userId, DateTime at);

        /// <summary>
        /// Validates a session and evaluates its current state, including expiration, revocation, and security version alignment.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="sessionId">The session identifier to validate.</param>
        /// <param name="now">The current UTC timestamp.</param>
        /// <returns>
        /// A detailed validation result describing the session, chain, root,
        /// and computed session state.
        /// </returns>
        Task<SessionValidationResult<TUserId>> ValidateSessionAsync(string? tenantId, AuthSessionId sessionId, DateTime now);

        /// <summary>
        /// Retrieves all session chains belonging to the specified user.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="userId">The user whose session chains are requested.</param>
        /// <returns>A read-only list of session chains.</returns>
        Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsAsync(string? tenantId, TUserId userId);

        /// <summary>
        /// Retrieves all sessions belonging to a specific session chain.
        /// </summary>
        /// <param name="tenantId">The tenant identifier, or <c>null</c>.</param>
        /// <param name="chainId">The session chain identifier.</param>
        /// <returns>A read-only list of sessions contained within the chain.</returns>
        Task<IReadOnlyList<ISession<TUserId>>> GetSessionsAsync(string? tenantId, ChainId chainId);
    }
}
