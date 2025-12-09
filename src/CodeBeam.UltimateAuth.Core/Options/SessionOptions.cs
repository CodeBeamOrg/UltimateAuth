namespace CodeBeam.UltimateAuth.Core.Options
{
    public sealed class SessionOptions
    {
        /// <summary>
        /// Base lifetime for a session (Level 3).
        /// </summary>
        public TimeSpan Lifetime { get; set; } = TimeSpan.FromDays(7);

        /// <summary>
        /// Hard cap lifetime — session cannot exist longer than this,
        /// even with sliding expiration.
        /// </summary>
        public TimeSpan MaxLifetime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// If true, every refresh extends the session lifetime.
        /// </summary>
        public bool SlidingExpiration { get; set; } = true;

        /// <summary>
        /// If user is idle longer than this period, session becomes invalid.
        /// If null or zero, idle timeout is disabled.
        /// </summary>
        public TimeSpan? IdleTimeout { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Maximum number of device chains per user.
        /// </summary>
        public int MaxChainsPerUser { get; set; } = 0;

        /// <summary>
        /// Maximum number of session rotations inside a chain.
        /// Used for cleanup and replay detection.
        /// </summary>
        public int MaxSessionsPerChain { get; set; } = 100;

        /// <summary>
        /// Limit number of chains per platform (e.g. web, mobile).
        /// </summary>
        public Dictionary<string, int>? MaxChainsPerPlatform { get; set; }

        /// <summary>
        /// Group platforms under categories.
        /// Example:
        /// mobile: [ ios, android, tablet ]
        /// </summary>
        public Dictionary<string, string[]>? PlatformCategories { get; set; }

        /// <summary>
        /// Limit number of chains per category (e.g. mobile, web).
        /// </summary>
        public Dictionary<string, int>? MaxChainsPerCategory { get; set; }

        /// <summary>
        /// Session is tied to user's IP address if enabled.
        /// </summary>
        public bool EnableIpBinding { get; set; } = false;

        /// <summary>
        /// Session is tied to user's User-Agent string if enabled.
        /// </summary>
        public bool EnableUserAgentBinding { get; set; } = false;
    }
}
