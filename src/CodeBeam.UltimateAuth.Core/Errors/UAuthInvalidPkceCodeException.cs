namespace CodeBeam.UltimateAuth.Core.Errors
{
    /// <summary>
    /// Represents an authentication failure occurring during the PKCE authorization
    /// flow when the supplied authorization code is invalid, expired, or does not
    /// match the original code challenge.
    /// This exception indicates a failed PKCE verification rather than a general
    /// credential or configuration error.
    /// </summary>
    public sealed class UAuthInvalidPkceCodeException : UAuthDomainException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UAuthInvalidPkceCodeException"/> class
        /// with a default message indicating an invalid PKCE authorization code.
        /// </summary>
        public UAuthInvalidPkceCodeException() : base("Invalid PKCE authorization code.")
        {
        }
    }
}
