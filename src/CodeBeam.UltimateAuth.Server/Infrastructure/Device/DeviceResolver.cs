using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

// TODO: Consider creating a seperate package with a library like UA Parser, WURFL or DeviceAtlas for more accurate device detection.
public sealed class DeviceResolver : IDeviceResolver
{
    private readonly IUserAgentParser _userAgentParser;

    public DeviceResolver(IUserAgentParser userAgentParser)
    {
        _userAgentParser = userAgentParser;
    }

    public async Task<DeviceInfo> ResolveAsync(HttpContext context)
    {
        var request = context.Request;

        var rawDeviceId = await ResolveRawDeviceId(context);
        if (!DeviceId.TryCreate(rawDeviceId, out var deviceId))
        {
            //throw new InvalidOperationException("device_id_required");
        }

        var ua = request.Headers.UserAgent.ToString();
        var parsed = _userAgentParser.Parse(ua);

        var deviceInfo = new DeviceInfo
        {
            DeviceId = deviceId,
            DeviceType = parsed.DeviceType,
            Platform = parsed.Platform,
            OperatingSystem = parsed.OperatingSystem,
            Browser = parsed.Browser,
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

    private static string? ResolveIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
