using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

// TODO: This is a very basic implementation.
// Consider creating a seperate package with a library like UA Parser, WURFL or DeviceAtlas for more accurate device detection. (Add IDeviceInfoParser)
public sealed class DeviceResolver : IDeviceResolver
{
    public async Task<DeviceInfo> ResolveAsync(HttpContext context)
    {
        var request = context.Request;

        var rawDeviceId = await ResolveRawDeviceId(context);
        if (!DeviceId.TryCreate(rawDeviceId, out var deviceId))
        {
            //throw new InvalidOperationException("device_id_required");
        }

        var ua = request.Headers.UserAgent.ToString();
        var deviceInfo = new DeviceInfo
        {
            DeviceId = deviceId,
            Platform = ResolvePlatform(ua),
            OperatingSystem = ResolveOperatingSystem(ua),
            Browser = ResolveBrowser(ua),
            UserAgent = ua,
            IpAddress = ResolveIp(context)
        };

        return deviceInfo;
    }

    private static async Task<string?> ResolveRawDeviceId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-UDID", out var header))
            return header.ToString();

        if (context.Request.HasFormContentType)
        {
            var form = await context.GetCachedFormAsync();

            if (form is not null &&
                form.TryGetValue(UAuthConstants.Form.Device, out var formValue) &&
                !StringValues.IsNullOrEmpty(formValue))
            {
                return formValue.ToString();
            }
        }

        if (context.Request.Cookies.TryGetValue("udid", out var cookie))
            return cookie;

        return null;
    }

    private static string? ResolvePlatform(string ua)
    {
        var s = ua.ToLowerInvariant();

        if (s.Contains("ipad") || s.Contains("tablet") || s.Contains("sm-t") /* bazı samsung tabletler */)
            return "tablet";

        if (s.Contains("mobi") || s.Contains("iphone") || s.Contains("android"))
            return "mobile";

        return "desktop";
    }

    private static string? ResolveOperatingSystem(string ua)
    {
        var s = ua.ToLowerInvariant();

        if (s.Contains("iphone") || s.Contains("ipad") || s.Contains("cpu os") || s.Contains("ios"))
            return "ios";

        if (s.Contains("android"))
            return "android";

        if (s.Contains("windows nt"))
            return "windows";

        if (s.Contains("mac os x") || s.Contains("macintosh"))
            return "macos";

        if (s.Contains("linux"))
            return "linux";

        return "unknown";
    }

    private static string? ResolveBrowser(string ua)
    {
        var s = ua.ToLowerInvariant();

        if (s.Contains("edg/"))
            return "edge";

        if (s.Contains("opr/") || s.Contains("opera"))
            return "opera";

        if (s.Contains("chrome/") && !s.Contains("chromium/"))
            return "chrome";

        if (s.Contains("safari/") && !s.Contains("chrome/") && !s.Contains("crios/"))
            return "safari";

        if (s.Contains("firefox/"))
            return "firefox";

        return "unknown";
    }

    private static string? ResolveIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
