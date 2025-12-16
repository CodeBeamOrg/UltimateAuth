namespace CodeBeam.UltimateAuth.Core.Contracts
{
    /// <summary>
    /// Request to register a new user with credentials.
    /// </summary>
    public sealed class RegisterUserRequest
    {
        /// <summary>
        /// Unique user identifier (username, email, or external id).
        /// Interpretation is application-specific.
        /// </summary>
        public required string Identifier { get; init; }

        /// <summary>
        /// Plain-text password.
        /// Will be hashed by the configured password hasher.
        /// </summary>
        public required string Password { get; init; }

        /// <summary>
        /// Optional tenant identifier.
        /// </summary>
        public string? TenantId { get; init; }

        /// <summary>
        /// Optional initial claims or metadata.
        /// </summary>
        public IReadOnlyDictionary<string, object>? Metadata { get; init; }
    }
}
