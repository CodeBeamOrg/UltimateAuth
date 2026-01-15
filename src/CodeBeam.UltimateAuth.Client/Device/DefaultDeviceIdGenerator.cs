using CodeBeam.UltimateAuth.Client.Device;
using CodeBeam.UltimateAuth.Core.Domain;
using System.Security.Cryptography;

namespace CodeBeam.UltimateAuth.Client.Devices;

public sealed class DefaultDeviceIdGenerator : IDeviceIdGenerator
{
    //public DeviceId Generate()
    //{
    //    Span<byte> buffer = stackalloc byte[32];
    //    RandomNumberGenerator.Fill(buffer);

    //    var raw = Convert.ToBase64String(buffer);
    //    return DeviceId.Create(raw);
    //}

    public DeviceId Generate()
    {
        Span<byte> buffer = stackalloc byte[32];
        RandomNumberGenerator.Fill(buffer);
        return DeviceId.CreateFromBytes(buffer);
    }

}
