using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public static class DeviceInfoFactory
    {
        public static DeviceInfo FromHttpContext(HttpContext context)
        {
            return new DeviceInfo
            {
                DeviceId = ResolveDeviceId(context),
                Platform = context.Request.Headers.UserAgent.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString()
            };
        }

        private static string ResolveDeviceId(HttpContext context)
        {
            // TODO: cookie / fingerprint / header in future
            return context.Request.Headers.UserAgent.ToString();
        }
    }
}
