namespace CodeBeam.UltimateAuth.Core.Errors
{
    /// <summary>
    /// Represents a domain-level authentication failure caused by a mismatch
    /// between the session's stored security version and the user's current
    /// security version.
    /// A mismatch indicates that a critical security event has occurred
    /// after the session was created—such as a password reset, MFA reset,
    /// account recovery, or other action requiring all prior sessions
    /// to be invalidated.
    /// </summary>
    public sealed class UAuthSecurityVersionMismatchException : UAuthDomainException
    {
        /// <summary>
        /// Gets the security version captured when the session was created.
        /// </summary>
        public long SessionVersion { get; }

        /// <summary>
        /// Gets the user's current security version, which has increased
        /// since the session was issued.
        /// </summary>
        public long UserVersion { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UAuthSecurityVersionMismatchException"/> class
        /// using the session's stored version and the user's current version.
        /// </summary>
        /// <param name="sessionVersion">The security version value stored in the session.</param>
        /// <param name="userVersion">The user's current security version.</param>
        public UAuthSecurityVersionMismatchException(long sessionVersion, long userVersion) : base($"Security version mismatch. Session={sessionVersion}, User={userVersion}")
        {
            SessionVersion = sessionVersion;
            UserVersion = userVersion;
        }
    }
}
