namespace CodeBeam.UltimateAuth.Core.Errors
{
    /// <summary>
    /// Represents an authentication failure caused by invalid user credentials.
    /// This error is thrown when the supplied username, password, or login
    /// identifier does not match any valid user account.
    /// </summary>
    public sealed class UAuthInvalidCredentialsException : UAuthDomainException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UAuthInvalidCredentialsException"/> class
        /// with a default message indicating incorrect credentials.
        /// </summary>
        public UAuthInvalidCredentialsException() : base("Invalid username or password.")
        {
        }
    }
}
