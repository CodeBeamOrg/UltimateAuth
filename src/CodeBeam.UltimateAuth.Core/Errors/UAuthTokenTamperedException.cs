namespace CodeBeam.UltimateAuth.Core.Errors
{
    /// <summary>
    /// Represents an authentication-domain exception thrown when a token fails its
    /// integrity verification checks, indicating that the token may have been altered,
    /// corrupted, or tampered with after issuance.
    ///
    /// This exception is raised during token validation when signature verification fails,
    /// claims are inconsistent, or protected fields do not match their expected values.
    /// Such failures generally imply either client-side manipulation or
    /// man-in-the-middle interference.
    ///
    /// Applications catching this exception should treat the associated token as unsafe
    /// and deny access immediately. Reauthentication or complete session invalidation
    /// may be required depending on the security policy.
    /// </summary>
    public sealed class UAuthTokenTamperedException : UAuthDomainException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UAuthTokenTamperedException"/> class.
        /// </summary>
        public UAuthTokenTamperedException() : base("Token integrity check failed (possible tampering).")
        {
        }
    }
}
