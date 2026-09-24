using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Device;

/// <summary>
/// Represents a generator for device identifiers.
/// </summary>
public interface IDeviceIdGenerator
{
    /// <summary>
    /// Generates a new device identifier.
    /// </summary>
    DeviceId Generate();
}
