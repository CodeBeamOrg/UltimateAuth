using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using System.Security;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal sealed class DefaultDeviceContextFactory : IDeviceContextFactory
    {
        public DeviceContext Create(DeviceInfo device)
        {
            if (string.IsNullOrWhiteSpace(device.DeviceId.Value))
                throw new SecurityException("DeviceId is required.");

            return new DeviceContext
            {
                DeviceId = device.DeviceId
            };
        }
    }
}
