namespace CodeBeam.UltimateAuth.Client.Options;

/// <summary>
/// Controls automatic background refresh behavior.
/// This does NOT guarantee session continuity.
/// </summary>
public sealed class UAuthClientAutoRefreshOptions
{
    /// <summary>
    /// Enables background refresh coordination.
    /// Default: true for BlazorServer, false otherwise.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval for background refresh attempts.
    /// This is a UX / keep-alive setting, NOT a security policy.
    /// </summary>
    public TimeSpan? Interval { get; set; }

    /// <summary>
    /// Enables automatic authentication recovery following a recoverable authentication failure.
    /// </summary>
    public bool Reactive { get; set; } = true;

    /// <summary>
    /// Enables expiration-aware access token refresh before authenticated requests.
    /// </summary>
    public bool Proactive { get; set; } = true;

    /// <summary>
    /// Determines how early an access token becomes eligible for proactive refresh.
    /// </summary>
    public TimeSpan RefreshBeforeExpiration { get; set; } = TimeSpan.FromMinutes(1);

    // TODO: Future enhancement: Add jitter to avoid synchronized refresh storms in multi-tab scenarios.
    ///// <summary>
    ///// Optional jitter to avoid synchronized refresh storms.
    ///// </summary>
    //public TimeSpan? Jitter { get; set; }
}
