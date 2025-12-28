using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Server.Extensions
{
    public static class DeviceExtensions
    {
        public static DeviceInfo GetDevice(this HttpContext context)
        {
            var resolver = context.RequestServices
                .GetRequiredService<IDeviceResolver>();

            return resolver.Resolve(context);
        }
    }
}
