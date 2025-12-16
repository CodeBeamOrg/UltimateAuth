namespace CodeBeam.UltimateAuth.Core
{
    /// <summary>
    /// Defines the authentication execution model for UltimateAuth.
    /// Each mode represents a fundamentally different security
    /// and lifecycle strategy.
    /// </summary>
    public enum UAuthMode
    {
        /// <summary>
        /// Pure opaque, session-based authentication.
        /// No JWT, no refresh token.
        /// Full server-side control with sliding expiration.
        /// Best for Blazor Server, MVC, intranet apps.
        /// </summary>
        PureOpaque = 0,

        /// <summary>
        /// Full hybrid mode.
        /// Session + JWT + refresh token.
        /// Server-side session control with JWT performance.
        /// Default mode.
        /// </summary>
        Hybrid = 1,

        /// <summary>
        /// Semi-hybrid mode.
        /// JWT is fully stateless at runtime.
        /// Session exists only as metadata/control plane
        /// (logout, disable, audit, device tracking).
        /// No request-time session lookup.
        /// </summary>
        SemiHybrid = 2,

        /// <summary>
        /// Pure JWT mode.
        /// Fully stateless authentication.
        /// No session, no server-side lookup.
        /// Revocation only via token expiration.
        /// </summary>
        PureJwt = 3
    }
}
