using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class DeviceContextFactory : IDeviceContextFactory
{
    public DeviceContext Create(DeviceInfo device)
    {
        if (string.IsNullOrWhiteSpace(device.DeviceId.Value))
            return DeviceContext.Anonymous();

        return DeviceContext.FromDeviceId(device.DeviceId);
    }
}
