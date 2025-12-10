namespace CodeBeam.UltimateAuth.Core.Errors
{
    /// <summary>
    /// Represents an unexpected internal error within the UltimateAuth framework.
    /// This exception indicates a failure in internal logic, invariants, or service
    /// coordination, rather than a configuration or authentication mistake by the developer.
    /// 
    /// If this exception occurs, it typically means a bug or unhandled scenario
    /// exists inside the framework itself.
    /// </summary>
    public sealed class UAuthInternalException : UAuthDeveloperException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UAuthInternalException"/> class
        /// with a descriptive message explaining the internal framework error.
        /// </summary>
        /// <param name="message">The internal error message.</param>
        public UAuthInternalException(string message) : base(message) { }
    }
}
