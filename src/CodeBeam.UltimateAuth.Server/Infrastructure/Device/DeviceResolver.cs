using CodeBeam.UltimateAuth.Core.Constants;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class DeviceResolver : IDeviceResolver
{
    public DeviceInfo Resolve(HttpContext context)
    {
        var request = context.Request;

        var rawDeviceId = ResolveRawDeviceId(context);
        DeviceId.TryCreate(rawDeviceId, out var deviceId);

        return new DeviceInfo
        {
            DeviceId = deviceId,
            Platform = ResolvePlatform(request),
            UserAgent = request.Headers.UserAgent.ToString(),
            IpAddress = context.Connection.RemoteIpAddress?.ToString()
        };
    }


    private static string? ResolveRawDeviceId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-UDID", out var header))
            return header.ToString();

        if (context.Request.HasFormContentType && context.Request.Form.TryGetValue(UAuthConstants.Form.Device, out var formValue) && !StringValues.IsNullOrEmpty(formValue))
        {
            return formValue.ToString();
        }

        if (context.Request.Cookies.TryGetValue("udid", out var cookie))
            return cookie;

        return null;
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
