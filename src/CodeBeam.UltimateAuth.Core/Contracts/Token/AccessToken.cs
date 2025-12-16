namespace CodeBeam.UltimateAuth.Core.Contracts
{
    /// <summary>
    /// Represents an issued access token (JWT or opaque).
    /// </summary>
    public sealed class AccessToken
    {
        /// <summary>
        /// The actual token value sent to the client.
        /// </summary>
        public required string Token { get; init; }

        // TODO: TokenKind enum?
        /// <summary>
        /// Token type: "jwt" or "opaque".
        /// Used for diagnostics and middleware behavior.
        /// </summary>
        public TokenType Type { get; init; }

        /// <summary>
        /// Expiration time of the token.
        /// </summary>
        public required DateTimeOffset ExpiresAt { get; init; }

        /// <summary>
        /// Optional session id this token is bound to (Hybrid / SemiHybrid).
        /// </summary>
        public string? SessionId { get; init; }

        public string? Scope { get; init; }
    }
}
