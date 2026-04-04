using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class DeviceContextFactory : IDeviceContextFactory
{
    public DeviceContext Create(DeviceInfo device)
    {
        if (device is null || string.IsNullOrWhiteSpace(device.DeviceId.Value))
            return DeviceContext.Anonymous();

        return DeviceContext.Create(
            deviceId: device.DeviceId,
            deviceType: device.DeviceType,
            platform: device.Platform,
            operatingSystem: device.OperatingSystem,
            browser: device.Browser,
            ipAddress: device.IpAddress
        );
    }
}
