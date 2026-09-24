using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Device;
using CodeBeam.UltimateAuth.Client.Infrastructure;

namespace CodeBeam.UltimateAuth.Client.Blazor.Device;

/// <summary>
/// Represents a device ID storage implementation that uses browser client storage to persist the device ID.
/// </summary>
public sealed class BrowserDeviceIdStorage : IDeviceIdStorage
{
    private const string Key = "udid";
    private readonly IClientStorage _storage;

    /// <inheritdoc />
    public BrowserDeviceIdStorage(IClientStorage storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// Loads the device ID from the browser client storage.
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async ValueTask<string?> LoadAsync(CancellationToken ct = default)
    {
        try
        {
            if (!await _storage.ExistsAsync(StorageScope.Local, Key))
                return null;

            return await _storage.GetAsync(StorageScope.Local, Key);
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// Saves the device ID to the browser client storage.
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public ValueTask SaveAsync(string deviceId, CancellationToken ct = default)
    {
        return _storage.SetAsync(StorageScope.Local, Key, deviceId);
    }
}
