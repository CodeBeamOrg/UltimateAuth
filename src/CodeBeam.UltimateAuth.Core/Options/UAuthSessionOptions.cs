using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Options;

// TODO: Add rotate on refresh (especially for Hybrid). Default behavior should be single session in chain for Hybrid, but can be configured.
// And add RotateAsync method.

/// <summary>
/// Defines configuration settings that control the lifecycle,
/// security behavior, and device constraints of UltimateAuth
/// session management.
/// 
/// These values influence how sessions are created, refreshed,
/// expired, revoked, and grouped into device chains.
/// </summary>
public sealed class UAuthSessionOptions
{
    /// <summary>
    /// The standard lifetime of a session before it expires.
    /// This is the duration added during login or refresh.
    /// </summary>
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Maximum absolute lifetime a session may have, even when
    /// sliding expiration is enabled. If null, no hard cap
    /// is applied.
    /// </summary>
    public TimeSpan? MaxLifetime { get; set; }

    /// <summary>
    /// When enabled, each refresh extends the session's expiration,
    /// allowing continuous usage until MaxLifetime or idle rules apply.
    /// On PureOpaque (or one token usage) mode, each touch restarts the session lifetime.
    /// </summary>
    public bool SlidingExpiration { get; set; } = true;

    /// <summary>
    /// Maximum allowed idle time before the session becomes invalid.
    /// If null or zero, idle expiration is disabled entirely.
    /// </summary>
    public TimeSpan? IdleTimeout { get; set; }

    /// <summary>
    /// Minimum interval between LastSeenAt updates.
    /// Prevents excessive writes under high traffic.
    /// </summary>
    public TimeSpan? TouchInterval { get; set; } = TimeSpan.FromMinutes(5);

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

    public DeviceMismatchBehavior DeviceMismatchBehavior { get; set; } = DeviceMismatchBehavior.Reject;

    internal UAuthSessionOptions Clone() => new()
    {
        SlidingExpiration = SlidingExpiration,
        IdleTimeout = IdleTimeout,
        Lifetime = Lifetime,
        MaxLifetime = MaxLifetime,
        TouchInterval = TouchInterval,
        DeviceMismatchBehavior = DeviceMismatchBehavior,
        MaxChainsPerUser = MaxChainsPerUser,
        MaxSessionsPerChain = MaxSessionsPerChain,
        MaxChainsPerPlatform = MaxChainsPerPlatform is null ? null : new Dictionary<string, int>(MaxChainsPerPlatform),
        MaxChainsPerCategory = MaxChainsPerCategory is null ? null : new Dictionary<string, int>(MaxChainsPerCategory),
        PlatformCategories = PlatformCategories is null ? null : PlatformCategories.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray()),
        EnableIpBinding = EnableIpBinding,
        EnableUserAgentBinding = EnableUserAgentBinding
    };

}
