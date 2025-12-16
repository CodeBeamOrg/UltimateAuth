using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class DefaultDeviceResolver : IDeviceResolver
    {
        public DeviceInfo Resolve(HttpContext context)
        {
            var request = context.Request;

            return new DeviceInfo
            {
                DeviceId = ResolveDeviceId(context),
                Platform = ResolvePlatform(request),
                OperatingSystem = null, // optional UA parsing later
                Browser = request.Headers.UserAgent.ToString(),
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = request.Headers.UserAgent.ToString(),
                IsTrusted = null
            };
        }

        private static string ResolveDeviceId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-Device-Id", out var header))
                return header.ToString();

            if (context.Request.Cookies.TryGetValue("ua_device", out var cookie))
                return cookie;

            return "unknown";
        }

        private static string? ResolvePlatform(HttpRequest request)
        {
            var ua = request.Headers.UserAgent.ToString().ToLowerInvariant();

            if (ua.Contains("android")) return "android";
            if (ua.Contains("iphone") || ua.Contains("ipad")) return "ios";
            if (ua.Contains("windows")) return "windows";
            if (ua.Contains("mac os")) return "macos";
            if (ua.Contains("linux")) return "linux";

            return "web";
        }
    }
}
