using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Infrastructure;

namespace CodeBeam.UltimateAuth.Client.Device;

public sealed class BrowserDeviceIdStorage : IDeviceIdStorage
{
    private const string Key = "udid";
    private readonly IBrowserStorage _storage;

    public BrowserDeviceIdStorage(IBrowserStorage storage)
    {
        _storage = storage;
    }

    public async ValueTask<string?> LoadAsync(CancellationToken ct = default)
    {
        if (!await _storage.ExistsAsync(StorageScope.Local, Key))
            return null;

        return await _storage.GetAsync(StorageScope.Local, Key);
    }

    public ValueTask SaveAsync(string deviceId, CancellationToken ct = default)
    {
        return _storage.SetAsync(StorageScope.Local, Key, deviceId);
    }
}
