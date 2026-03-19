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

    // TODO: Future enhancement: Add jitter to avoid synchronized refresh storms in multi-tab scenarios.
    ///// <summary>
    ///// Optional jitter to avoid synchronized refresh storms.
    ///// </summary>
    //public TimeSpan? Jitter { get; set; }
}
