namespace CodeBeam.UltimateAuth.Client.Device
{
    public interface IDeviceIdStorage
    {
        ValueTask<string?> LoadAsync(CancellationToken ct = default);
        ValueTask SaveAsync(string deviceId, CancellationToken ct = default);
    }
}
