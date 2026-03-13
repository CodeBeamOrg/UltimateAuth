using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed class DeviceInfo
{
    public required DeviceId DeviceId { get; init; }

    // TODO: Implement device type and device limits
    /// <summary>
    /// Device type that can be used for device limits. Sends with header "X-Device-Type" or form field "device_type". Examples: "web", "mobile", "desktop", "tablet", "iot".
    /// </summary>
    public string? DeviceType { get; init; }

    /// <summary>
    /// Operating system information (e.g. iOS 17, Android 14, Windows 11).
    /// </summary>
    public string? OperatingSystem { get; init; }

    /// <summary>
    /// Browser name/version for web clients.
    /// </summary>
    public string? Browser { get; init; }

    /// <summary>
    /// High-level platform classification (web, mobile, desktop, iot).
    /// Used for analytics and policy decisions.
    /// </summary>
    public string? Platform { get; init; }

    /// <summary>
    /// Raw user-agent string (optional).
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// Client IP address at session creation or last validation.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Optional fingerprint hash provided by client.
    /// Not trusted by default.
    /// </summary>
    public string? Fingerprint { get; init; }

    /// <summary>
    /// Arbitrary metadata for future extensions.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
