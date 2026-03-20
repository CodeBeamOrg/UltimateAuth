using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Device;
using CodeBeam.UltimateAuth.Client.Infrastructure;

namespace CodeBeam.UltimateAuth.Client.Blazor.Device;

public sealed class BrowserDeviceIdStorage : IDeviceIdStorage
{
    private const string Key = "udid";
    private readonly IClientStorage _storage;

    public BrowserDeviceIdStorage(IClientStorage storage)
    {
        _storage = storage;
    }

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

    public ValueTask SaveAsync(string deviceId, CancellationToken ct = default)
    {
        return _storage.SetAsync(StorageScope.Local, Key, deviceId);
    }
}
