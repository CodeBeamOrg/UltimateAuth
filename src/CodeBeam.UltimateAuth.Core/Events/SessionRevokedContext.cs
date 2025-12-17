using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Events
{
    /// <summary>
    /// Represents contextual data emitted when an individual session is revoked.
    /// 
    /// This event is triggered when a specific session is invalidated — either due to
    /// explicit logout, administrator action, security enforcement, or anomaly detection.
    /// Only the targeted session is revoked; other sessions in the same chain or root
    /// may continue to remain active unless broader revocation policies apply.
    /// 
    /// Typical use cases include:
    /// - Auditing and compliance logs
    /// - User notifications (e.g., “Your session on device X was logged out”)
    /// - Security automations (SIEM integration, monitoring suspicious activity)
    /// - Application workflows that must respond to session termination
    /// </summary>
    public sealed class SessionRevokedContext<TUserId> : IAuthEventContext
    {
        /// <summary>
        /// Gets the identifier of the user to whom the revoked session belongs.
        /// </summary>
        public TUserId UserId { get; }

        /// <summary>
        /// Gets the identifier of the session that has been revoked.
        /// </summary>
        public AuthSessionId SessionId { get; }

        /// <summary>
        /// Gets the identifier of the session chain containing the revoked session.
        /// </summary>
        public ChainId ChainId { get; }

        /// <summary>
        /// Gets the timestamp at which the session revocation occurred.
        /// </summary>
        public DateTimeOffset RevokedAt { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionRevokedContext{TUserId}"/> class.
        /// </summary>
        public SessionRevokedContext(
            TUserId userId,
            AuthSessionId sessionId,
            ChainId chainId,
            DateTimeOffset revokedAt)
        {
            UserId = userId;
            SessionId = sessionId;
            ChainId = chainId;
            RevokedAt = revokedAt;
        }
    }

}
