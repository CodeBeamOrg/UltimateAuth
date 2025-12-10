using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Events
{
    /// <summary>
    /// Represents contextual data emitted when an authentication session is refreshed.
    /// 
    /// This event occurs whenever a valid session performs a rotation — typically during
    /// a refresh-token exchange or session renewal flow. The old session becomes inactive,
    /// and a new session inherits updated expiration and security metadata.
    /// 
    /// This event is primarily used for analytics, auditing, security monitoring, and
    /// external workflow triggers (e.g., notifying users of new logins, updating dashboards,
    /// or tracking device activity).
    /// </summary>
    public sealed class SessionRefreshedContext<TUserId> : IAuthEventContext
    {
        /// <summary>
        /// Gets the identifier of the user whose session was refreshed.
        /// </summary>
        public TUserId UserId { get; }

        /// <summary>
        /// Gets the identifier of the session that was replaced during the refresh operation.
        /// </summary>
        public AuthSessionId OldSessionId { get; }

        /// <summary>
        /// Gets the identifier of the newly created session that replaces the old session.
        /// </summary>
        public AuthSessionId NewSessionId { get; }

        /// <summary>
        /// Gets the identifier of the session chain to which both sessions belong.
        /// </summary>
        public ChainId ChainId { get; }

        /// <summary>
        /// Gets the timestamp at which the refresh occurred.
        /// </summary>
        public DateTime RefreshedAt { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionRefreshedContext{TUserId}"/> class.
        /// </summary>
        public SessionRefreshedContext(
            TUserId userId,
            AuthSessionId oldSessionId,
            AuthSessionId newSessionId,
            ChainId chainId,
            DateTime refreshedAt)
        {
            UserId = userId;
            OldSessionId = oldSessionId;
            NewSessionId = newSessionId;
            ChainId = chainId;
            RefreshedAt = refreshedAt;
        }
    }
}
