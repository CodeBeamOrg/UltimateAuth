namespace CodeBeam.UltimateAuth.Core.Domain
{
    /// <summary>
    /// Represents additional metadata attached to an authentication session.
    /// This information is application-defined and commonly used for analytics,
    /// UI adaptation, multi-tenant context, and CSRF/session-related security data.
    /// </summary>
    public sealed class SessionMetadata
    {
        /// <summary>
        /// Represents an empty or uninitialized session metadata instance.
        /// </summary>
        /// <remarks>Use this field to represent a default or non-existent session when no metadata is
        /// available. This instance contains default values for all properties and can be used for comparison or as a
        /// placeholder.</remarks>
        public static readonly SessionMetadata Empty = new SessionMetadata();

        /// <summary>
        /// Gets the version of the client application that created the session.
        /// Useful for enforcing upgrade policies or troubleshooting version-related issues.
        /// </summary>
        public string? AppVersion { get; init; }

        /// <summary>
        /// Gets the locale or culture identifier associated with the session,
        /// such as <c>en-US</c>, <c>tr-TR</c>, or <c>fr-FR</c>.
        /// </summary>
        public string? Locale { get; init; }

        /// <summary>
        /// Gets a Cross-Site Request Forgery token or other session-scoped secret
        /// used for request integrity validation in web applications.
        /// </summary>
        public string? CsrfToken { get; init; }

        /// <summary>
        /// Gets a dictionary for storing arbitrary application-defined metadata.
        /// Allows extensions without modifying the core authentication model.
        /// </summary>
        public Dictionary<string, object>? Custom { get; init; }
    }
}
