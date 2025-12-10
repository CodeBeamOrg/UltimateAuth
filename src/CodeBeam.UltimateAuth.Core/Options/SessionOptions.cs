namespace CodeBeam.UltimateAuth.Core.Options
{
    /// <summary>
    /// Defines configuration settings that control the lifecycle,
    /// security behavior, and device constraints of UltimateAuth
    /// session management.
    /// 
    /// These values influence how sessions are created, refreshed,
    /// expired, revoked, and grouped into device chains.
    /// </summary>
    public sealed class SessionOptions
    {
        /// <summary>
        /// The standard lifetime of a session before it expires.
        /// This is the duration added during login or refresh.
        /// </summary>
        public TimeSpan Lifetime { get; set; } = TimeSpan.FromDays(7);

        /// <summary>
        /// Maximum absolute lifetime a session may have, even when
        /// sliding expiration is enabled. If set to zero, no hard cap
        /// is applied.
        /// </summary>
        public TimeSpan MaxLifetime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// When enabled, each refresh extends the session's expiration,
        /// allowing continuous usage until MaxLifetime or idle rules apply.
        /// </summary>
        public bool SlidingExpiration { get; set; } = true;

        /// <summary>
        /// Maximum allowed idle time before the session becomes invalid.
        /// If null or zero, idle expiration is disabled entirely.
        /// </summary>
        public TimeSpan? IdleTimeout { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Maximum number of device session chains a single user may have.
        /// Set to zero to indicate no user-level chain limit.
        /// </summary>
        public int MaxChainsPerUser { get; set; } = 0;

        /// <summary>
        /// Maximum number of session rotations within a single chain.
        /// Used for cleanup, replay protection, and analytics.
        /// </summary>
        public int MaxSessionsPerChain { get; set; } = 100;

        /// <summary>
        /// Optional limit on the number of session chains allowed per platform
        /// (e.g. "web" = 1, "mobile" = 1).
        /// </summary>
        public Dictionary<string, int>? MaxChainsPerPlatform { get; set; }

        /// <summary>
        /// Defines platform categories that map multiple platforms
        /// into a single abstract group (e.g. mobile: [ "ios", "android", "tablet" ]).
        /// </summary>
        public Dictionary<string, string[]>? PlatformCategories { get; set; }

        /// <summary>
        /// Limits how many session chains can exist per platform category
        /// (e.g. mobile = 1, desktop = 2).
        /// </summary>
        public Dictionary<string, int>? MaxChainsPerCategory { get; set; }

        /// <summary>
        /// Enables binding sessions to the user's IP address.
        /// When enabled, IP mismatches can invalidate a session.
        /// </summary>
        public bool EnableIpBinding { get; set; } = false;

        /// <summary>
        /// Enables binding sessions to the user's User-Agent header.
        /// When enabled, UA mismatches can invalidate a session.
        /// </summary>
        public bool EnableUserAgentBinding { get; set; } = false;
    }
}
