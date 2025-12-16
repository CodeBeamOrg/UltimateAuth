namespace CodeBeam.UltimateAuth.Core.Contracts
{
    /// <summary>
    /// Represents an issued refresh token.
    /// Always opaque and hashed at rest.
    /// </summary>
    public sealed class RefreshToken
    {
        /// <summary>
        /// Plain refresh token value (returned to client once).
        /// </summary>
        public required string Token { get; init; }

        /// <summary>
        /// Hash of the refresh token to be persisted.
        /// </summary>
        public required string TokenHash { get; init; }

        /// <summary>
        /// Expiration time.
        /// </summary>
        public required DateTimeOffset ExpiresAt { get; init; }
    }
}
