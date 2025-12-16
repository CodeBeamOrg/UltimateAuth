namespace CodeBeam.UltimateAuth.Core.Contracts
{
    /// <summary>
    /// Request to validate user credentials.
    /// Used during login flows.
    /// </summary>
    public sealed class ValidateCredentialsRequest
    {
        /// <summary>
        /// User identifier (same value used during registration).
        /// </summary>
        public required string Identifier { get; init; }

        /// <summary>
        /// Plain-text password provided by the user.
        /// </summary>
        public required string Password { get; init; }

        /// <summary>
        /// Optional tenant identifier.
        /// </summary>
        public string? TenantId { get; init; }
    }
}
