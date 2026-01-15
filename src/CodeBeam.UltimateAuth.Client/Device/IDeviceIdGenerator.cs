using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Device
{
    public interface IDeviceIdGenerator
    {
        DeviceId Generate();
    }
}
