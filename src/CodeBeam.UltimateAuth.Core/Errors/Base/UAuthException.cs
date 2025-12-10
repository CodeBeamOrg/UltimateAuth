namespace CodeBeam.UltimateAuth.Core.Errors
{
    /// <summary>
    /// Represents the base type for all exceptions thrown by the UltimateAuth framework.
    /// This class differentiates authentication-domain errors from general system exceptions
    /// and provides a common abstraction for developer, domain, and runtime error types.
    /// </summary>
    public abstract class UAuthException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UAuthException"/> class
        /// with the specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        protected UAuthException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UAuthException"/> class
        /// with the specified error message and underlying exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The exception that caused the current error.</param>
        protected UAuthException(string message, Exception? inner) : base(message, inner) { }
    }
}
