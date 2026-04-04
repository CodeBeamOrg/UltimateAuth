using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Abstractions;

public interface IClientDeviceProvider
{
    Task<DeviceContext> GetAsync();
}
