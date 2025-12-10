namespace CodeBeam.UltimateAuth.Core.Errors
{
    /// <summary>
    /// Represents an exception that occurs when a session or user store
    /// behaves incorrectly or violates the UltimateAuth storage contract.
    /// This typically indicates an implementation error in the application's
    /// persistence layer rather than a framework or authentication issue.
    /// </summary>
    public sealed class UAuthStoreException : UAuthDeveloperException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UAuthStoreException"/> class
        /// with a descriptive message explaining the store failure.
        /// </summary>
        /// <param name="message">The message describing the store-related error.</param>
        public UAuthStoreException(string message) : base(message) { }
    }
}
