using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Events;

namespace CodeBeam.UltimateAuth.Core.Options
{
    /// <summary>
    /// Top-level configuration container for all UltimateAuth features.
    /// Combines login policies, session lifecycle rules, token behavior,
    /// PKCE settings, multi-tenancy behavior, and user-id normalization.
    /// 
    /// All sub-options are resolved from configuration (appsettings.json)
    /// or through inline setup in AddUltimateAuth().
    /// </summary>
    public sealed class UltimateAuthOptions
    {
        /// <summary>
        /// Configuration settings for interactive login flows,
        /// including lockout thresholds and failed-attempt policies.
        /// </summary>
        public LoginOptions Login { get; set; } = new();

        /// <summary>
        /// Settings that control session creation, refresh behavior,
        /// sliding expiration, idle timeouts, device limits, and chain rules.
        /// </summary>
        public SessionOptions Session { get; set; } = new();

        /// <summary>
        /// Token issuance configuration, including JWT and opaque token
        /// generation, lifetimes, signing keys, and audience/issuer values.
        /// </summary>
        public TokenOptions Token { get; set; } = new();

        /// <summary>
        /// PKCE (Proof Key for Code Exchange) configuration used for
        /// browser-based login flows and WASM authentication.
        /// </summary>
        public PkceOptions Pkce { get; set; } = new();

        /// <summary>
        /// Event hooks raised during authentication lifecycle events
        /// such as login, logout, session creation, refresh, or revocation.
        /// </summary>
        public UAuthEvents UAuthEvents { get; set; } = new();

        /// <summary>
        /// Multi-tenancy configuration controlling how tenants are resolved,
        /// validated, and optionally enforced.
        /// </summary>
        public MultiTenantOptions MultiTenantOptions { get; set; } = new();

        /// <summary>
        /// Provides converters used to normalize and serialize TUserId
        /// across the system (sessions, stores, tokens, logging).
        /// </summary>
        public IUserIdConverterResolver? UserIdConverters { get; set; }
    }
}
