using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public interface IDeviceContextFactory
    {
        DeviceContext Create(DeviceInfo requestDevice);
    }
}
