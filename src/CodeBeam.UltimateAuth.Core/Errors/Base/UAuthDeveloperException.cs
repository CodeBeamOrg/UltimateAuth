namespace CodeBeam.UltimateAuth.Core.Errors
{
    /// <summary>
    /// Represents an exception that indicates a developer integration error
    /// rather than a runtime or authentication failure.
    /// These errors typically occur when UltimateAuth is misconfigured,
    /// required services are not registered, or contracts are violated by the host application.
    /// </summary>
    public abstract class UAuthDeveloperException : UAuthException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UAuthDeveloperException"/> class
        /// with a specified error message describing the developer mistake.
        /// </summary>
        /// <param name="message">The error message explaining the incorrect usage.</param>
        protected UAuthDeveloperException(string message) : base(message) { }
    }
}
