namespace CodeBeam.UltimateAuth.Client.Device;

/// <summary>
/// Represents a storage mechanism for device identifiers.
/// </summary>
public interface IDeviceIdStorage
{
    /// <summary>
    /// Loads the device identifier asynchronously.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    ValueTask<string?> LoadAsync(CancellationToken ct = default);

    /// <summary>
    /// Saves the device identifier asynchronously.
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    ValueTask SaveAsync(string deviceId, CancellationToken ct = default);
}
