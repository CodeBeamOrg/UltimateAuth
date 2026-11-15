namespace CodeBeam.UltimateAuth.Server.Options;

/// <summary>
/// Configures global and per-type limits for active devices.
///
/// Devices are identified by distinct DeviceId values associated
/// with active session chains.
///
/// Device types are client classifications and do not establish
/// trusted device identity.
/// </summary>
public sealed class UAuthDeviceLimitOptions
{
    /// <summary>
    /// Maximum number of active devices per user and tenant.
    /// Null means no global limit.
    /// </summary>
    public int? MaxDevices { get; set; }

    /// <summary>
    /// Maximum number of active devices for each device type.
    ///
    /// Keys are case-insensitive device type names, such as
    /// web, mobile, tablet, desktop or custom application types.
    ///
    /// An empty dictionary means no per-type limits.
    /// </summary>
    public Dictionary<string, int> TypeLimits { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Determines how exceeding a configured limit is handled.
    /// </summary>
    public DeviceLimitBehavior Behavior { get; set; } = DeviceLimitBehavior.RejectNewLogin;

    internal bool HasLimits => MaxDevices.HasValue || TypeLimits.Count > 0;

    internal UAuthDeviceLimitOptions Clone() => new()
    {
        MaxDevices = MaxDevices,
        Behavior = Behavior,
        TypeLimits = new Dictionary<string, int>(TypeLimits, StringComparer.OrdinalIgnoreCase)
    };
}
