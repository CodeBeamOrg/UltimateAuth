using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Device
{
    public interface IDeviceIdProvider
    {
        ValueTask<DeviceId> GetOrCreateAsync(CancellationToken ct = default);
    }
}
