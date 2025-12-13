namespace CodeBeam.UltimateAuth.Core.Options
{
    /// <summary>
    /// Configuration settings for access and refresh token behavior
    /// within UltimateAuth. Includes JWT and opaque token generation,
    /// lifetimes, and cryptographic settings.
    /// </summary>
    public sealed class UAuthTokenOptions
    {
        /// <summary>
        /// Determines whether JWT-format access tokens should be issued.
        /// Recommended for APIs that rely on claims-based authorization.
        /// </summary>
        public bool IssueJwt { get; set; } = true;

        /// <summary>
        /// Determines whether opaque tokens (session-id based) should be issued.
        /// Useful for high-security APIs where token introspection is required.
        /// </summary>
        public bool IssueOpaque { get; set; } = true;

        /// <summary>
        /// Lifetime of access tokens (JWT or opaque).
        /// Short lifetimes improve security but require more frequent refreshes.
        /// </summary>
        public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Lifetime of refresh tokens, used in PKCE or session rotation flows.
        /// </summary>
        public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);

        /// <summary>
        /// Number of bytes of randomness used when generating opaque token IDs.
        /// Larger values increase entropy and reduce collision risk.
        /// </summary>
        public int OpaqueIdBytes { get; set; } = 32;

        /// <summary>
        /// Symmetric key used to sign JWT access tokens.
        /// Must be long and cryptographically strong.
        /// </summary>
        public string SigningKey { get; set; } = string.Empty!;

        /// <summary>
        /// Value assigned to the JWT "iss" (issuer) claim.
        /// Identifies the authority that issued the token.
        /// </summary>
        public string Issuer { get; set; } = "UAuth";

        /// <summary>
        /// Value assigned to the JWT "aud" (audience) claim.
        /// Controls which clients or APIs are permitted to consume the token.
        /// </summary>
        public string Audience { get; set; } = "UAuthClient";

        /// <summary>
        /// If true, adds a unique 'jti' (JWT ID) claim to each issued JWT.
        /// Useful for token replay detection and advanced auditing.
        /// </summary>
        public bool AddJwtIdClaim { get; set; } = false;
    }
}
