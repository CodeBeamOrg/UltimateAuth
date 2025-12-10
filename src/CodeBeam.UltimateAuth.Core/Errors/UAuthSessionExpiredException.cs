using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Errors
{
    /// <summary>
    /// Represents an authentication-domain exception thrown when a session
    /// has passed its expiration time.
    ///
    /// This exception is raised during validation or refresh attempts where
    /// the session's <see cref="ISession{TUserId}.ExpiresAt"/> timestamp
    /// indicates that it is no longer valid.  
    /// 
    /// Once expired, a session cannot be refreshed — the user must log in again.
    /// </summary>
    public sealed class UAuthSessionExpiredException : UAuthSessionException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UAuthSessionExpiredException"/> class
        /// using the expired session's identifier.
        /// </summary>
        /// <param name="sessionId">The identifier of the expired session.</param>
        public UAuthSessionExpiredException(AuthSessionId sessionId) : base(sessionId, $"Session '{sessionId}' has expired.")
        {
        }
    }
}
