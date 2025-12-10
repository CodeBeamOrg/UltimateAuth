namespace CodeBeam.UltimateAuth.Core.Errors
{
    /// <summary>
    /// Represents a domain-level authentication failure indicating that the user's
    /// entire session root has been revoked.
    /// When a root is revoked, all session chains and all sessions belonging to the
    /// user become immediately invalid, regardless of their individual expiration
    /// or revocation state.
    /// </summary>
    public sealed class UAuthRootRevokedException : UAuthDomainException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UAuthRootRevokedException"/> class
        /// with a default message indicating that all sessions under the root are invalid.
        /// </summary>
        public UAuthRootRevokedException() : base("User root has been revoked. All sessions are invalid.")
        {
        }
    }
}
