using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Server.Extensions;

public static class DeviceExtensions
{
    public static async Task<DeviceInfo> GetDeviceAsync(this HttpContext context)
    {
        var resolver = context.RequestServices.GetRequiredService<IDeviceResolver>();
        return await resolver.ResolveAsync(context);
    }
}
