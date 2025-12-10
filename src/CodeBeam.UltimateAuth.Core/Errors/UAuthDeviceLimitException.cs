namespace CodeBeam.UltimateAuth.Core.Errors
{
    /// <summary>
    /// Represents a domain-level exception that is thrown when a user exceeds the allowed number of device or platform-specific session chains.
    /// This typically occurs when UltimateAuth's session policy restricts the
    /// number of concurrent logins for a given platform (e.g., web, mobile)
    /// and the user attempts to create an additional session beyond the limit.
    /// </summary>
    public sealed class UAuthDeviceLimitException : UAuthDomainException
    {
        /// <summary>
        /// Gets the platform for which the device or session-chain limit was exceeded.
        /// </summary>
        public string Platform { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UAuthDeviceLimitException"/> class with the specified platform name.
        /// </summary>
        /// <param name="platform">The platform on which the limit was exceeded.</param>
        public UAuthDeviceLimitException(string platform) : base($"Device limit exceeded for platform '{platform}'.")
        {
            Platform = platform;
        }
    }
}
