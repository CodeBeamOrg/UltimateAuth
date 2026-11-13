namespace CodeBeam.UltimateAuth.Server.Options;

/// <summary>
/// Limits the resource usage of the built-in in-memory auth artifact store.
/// Custom IAuthStore implementations manage their own capacity and cleanup.
/// </summary>
public sealed class UAuthArtifactStoreOptions
{
    /// <summary>
    /// Maximum number of active artifacts retained in memory per process.
    /// </summary>
    public int MaxEntries { get; set; } = 50_000;

    /// <summary>
    /// Interval between background expiration sweeps.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(1);

    internal UAuthArtifactStoreOptions Clone() => new()
    {
        MaxEntries = MaxEntries,
        CleanupInterval = CleanupInterval
    };
}
