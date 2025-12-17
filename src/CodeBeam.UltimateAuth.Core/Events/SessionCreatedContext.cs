using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Events
{
    /// <summary>
    /// Represents contextual data emitted when a new authentication session is created.
    /// 
    /// This event is published immediately after a successful login or initial session
    /// creation within a session chain. It provides the essential identifiers required
    /// for auditing, monitoring, analytics, and external integrations.
    /// 
    /// Handlers should treat this event as notification-only; modifying session state
    /// or performing security-critical actions is not recommended unless explicitly intended.
    /// </summary>
    public sealed class SessionCreatedContext<TUserId> : IAuthEventContext
    {
        /// <summary>
        /// Gets the identifier of the user for whom the new session was created.
        /// </summary>
        public TUserId UserId { get; }

        /// <summary>
        /// Gets the unique identifier of the newly created session.
        /// </summary>
        public AuthSessionId SessionId { get; }

        /// <summary>
        /// Gets the identifier of the session chain to which this session belongs.
        /// </summary>
        public ChainId ChainId { get; }

        /// <summary>
        /// Gets the timestamp on which the session was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionCreatedContext{TUserId}"/> class.
        /// </summary>
        public SessionCreatedContext(TUserId userId, AuthSessionId sessionId, ChainId chainId, DateTimeOffset createdAt)
        {
            UserId = userId;
            SessionId = sessionId;
            ChainId = chainId;
            CreatedAt = createdAt;
        }
    }
}
