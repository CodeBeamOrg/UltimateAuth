using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client;

/// <summary>
/// Provides a mechanism to retrieve or create a unique device identifier for the client application.
/// </summary>
public interface IDeviceIdProvider
{
    /// <summary>
    /// Retrieves the existing device identifier or creates a new one if it doesn't exist.
    /// </summary>
    ValueTask<DeviceId> GetOrCreateAsync(CancellationToken ct = default);
}
