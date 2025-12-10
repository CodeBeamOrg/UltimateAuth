using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Errors
{
    /// <summary>
    /// Represents a domain-level exception associated with a specific authentication session.
    /// This error indicates that a session-related invariant or rule has been violated,
    /// such as attempting to refresh a revoked session, using an expired session,
    /// or performing an operation that conflicts with the session's current state.
    /// </summary>
    public abstract class UAuthSessionException : UAuthDomainException
    {
        /// <summary>
        /// Gets the identifier of the session that triggered the exception.
        /// </summary>
        public AuthSessionId SessionId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UAuthSessionException"/> class with the session identifier and an explanatory error message.
        /// </summary>
        /// <param name="sessionId">The session identifier associated with the error.</param>
        /// <param name="message">The message describing the session rule violation.</param>
        protected UAuthSessionException(AuthSessionId sessionId, string message) : base(message)
        {
            SessionId = sessionId;
        }
    }

}
