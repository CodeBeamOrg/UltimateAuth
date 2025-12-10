namespace CodeBeam.UltimateAuth.Core.Errors
{
    /// <summary>
    /// Represents an exception that is thrown when UltimateAuth is configured
    /// incorrectly or when required configuration values are missing or invalid.
    /// This error indicates a developer-side setup issue rather than a runtime
    /// authentication failure.
    /// </summary>
    public sealed class UAuthConfigException : UAuthDeveloperException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UAuthConfigException"/> class
        /// with a descriptive message explaining the configuration problem.
        /// </summary>
        /// <param name="message">The message describing the configuration error.</param>
        public UAuthConfigException(string message) : base(message) { }
    }
}
