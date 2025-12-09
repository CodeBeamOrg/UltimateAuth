namespace CodeBeam.UltimateAuth.Core.Options
{
    public sealed class TokenOptions
    {
        /// <summary>
        /// Issue JWT access tokens.
        /// </summary>
        public bool IssueJwt { get; set; } = true;

        /// <summary>
        /// Issue opaque access tokens (session-id based).
        /// </summary>
        public bool IssueOpaque { get; set; } = true;

        /// <summary>
        /// Lifetime of JWT (or opaque) access tokens.
        /// </summary>
        public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Lifetime of refresh tokens (PKCE or session rotation).
        /// </summary>
        public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);

        /// <summary>
        /// Length of opaque token (SessionId) entropy.
        /// </summary>
        public int OpaqueIdBytes { get; set; } = 32;

        /// <summary>
        /// JWT signing key (symmetric for now).
        /// </summary>
        public string SigningKey { get; set; } = string.Empty!;

        /// <summary>
        /// JWT issuer field.
        /// </summary>
        public string Issuer { get; set; } = "UAuth";

        /// <summary>
        /// JWT audience field.
        /// </summary>
        public string Audience { get; set; } = "UAuthClient";
    }
}
