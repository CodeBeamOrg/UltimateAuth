using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Core.Domain
{
    /// <summary>
    /// Framework-agnostic JWT description used by IJwtTokenGenerator.
    /// </summary>
    public sealed class UAuthJwtTokenDescriptor
    {
        public required ClaimsIdentity Subject { get; init; }

        public required string Issuer { get; init; }

        public required string Audience { get; init; }

        public required DateTimeOffset Expires { get; init; }

        /// <summary>
        /// Signing key material (symmetric or asymmetric).
        /// Interpretation is up to the generator implementation.
        /// </summary>
        public required object SigningKey { get; init; }
    }
}
