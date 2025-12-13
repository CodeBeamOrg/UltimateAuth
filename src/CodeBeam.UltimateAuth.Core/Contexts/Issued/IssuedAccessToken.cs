namespace CodeBeam.UltimateAuth.Core.Contexts
{
    /// <summary>
    /// Represents an issued access token (JWT or opaque).
    /// </summary>
    public sealed class IssuedAccessToken
    {
        /// <summary>
        /// The actual token value sent to the client.
        /// </summary>
        public required string Token { get; init; }

        /// <summary>
        /// Token type: "jwt" or "opaque".
        /// Used for diagnostics and middleware behavior.
        /// </summary>
        public required string TokenType { get; init; }

        /// <summary>
        /// Expiration time of the token.
        /// </summary>
        public required DateTimeOffset ExpiresAt { get; init; }

        /// <summary>
        /// Optional session id this token is bound to (Hybrid / SemiHybrid).
        /// </summary>
        public string? SessionId { get; init; }
    }
}
