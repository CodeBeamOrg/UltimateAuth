namespace CodeBeam.UltimateAuth.Core.Domain
{
    /// <summary>
    /// Represents metadata describing the device or client environment initiating
    /// an authentication session. Used for security analytics, session management,
    /// fraud detection, and device-specific login policies.
    /// </summary>
    public sealed class DeviceInfo
    {
        /// <summary>
        /// Gets the unique identifier for the device.
        /// </summary>
        public string DeviceId { get; init; } = default!;

        /// <summary>
        /// Gets the high-level platform identifier, such as <c>web</c>, <c>mobile</c>,
        /// <c>tablet</c> or <c>iot</c>.
        /// Used for platform-based session limits and analytics.
        /// </summary>
        public string? Platform { get; init; }

        /// <summary>
        /// Gets the operating system of the client device, such as <c>iOS 17</c>,
        /// <c>Android 14</c>, <c>Windows 11</c>, or <c>macOS Sonoma</c>.
        /// </summary>
        public string? OperatingSystem { get; init; }

        /// <summary>
        /// Gets the browser name and version when the client is web-based,
        /// such as <c>Edge</c>, <c>Chrome</c>, <c>Safari</c>, or <c>Firefox</c>.  
        /// May be <c>null</c> for native applications.
        /// </summary>
        public string? Browser { get; init; }

        /// <summary>
        /// Gets the IP address of the client device.  
        /// Used for IP-binding, geolocation checks, and anomaly detection.
        /// </summary>
        public string? IpAddress { get; init; }

        /// <summary>
        /// Gets the raw user-agent string for web clients.  
        /// Used when deeper parsing of browser or device details is needed.
        /// </summary>
        public string? UserAgent { get; init; }

        /// <summary>
        /// Gets a device fingerprint or unique client identifier if provided by the
        /// application. Useful for advanced session policies or fraud analysis.
        /// </summary>
        public string? Fingerprint { get; init; }

        /// <summary>
        /// Indicates whether the device is considered trusted by the user or system.
        /// Applications may update this value when implementing trusted-device flows.
        /// </summary>
        public bool? IsTrusted { get; init; }

        /// <summary>
        /// Gets optional custom metadata supplied by the application.  
        /// Allows additional device attributes not covered by standard fields.
        /// </summary>
        public Dictionary<string, string>? Custom { get; init; }

        public static DeviceInfo Unknown { get; } = new()
        {
            DeviceId = "unknown",
            Platform = null,
            Browser = null,
            IpAddress = null,
            UserAgent = null,
            IsTrusted = null
        };

        /// <summary>
        /// Determines whether the current device information matches the specified device information based on device
        /// identifiers.
        /// </summary>
        /// <param name="other">The device information to compare with the current instance. Cannot be null.</param>
        /// <returns>true if the device identifiers are equal; otherwise, false.</returns>
        public bool Matches(DeviceInfo other)
        {
            if (other is null)
                return false;

            if (DeviceId != other.DeviceId)
                return false;

            // TODO: UA / IP drift policy
            return true;
        }
    }
}
