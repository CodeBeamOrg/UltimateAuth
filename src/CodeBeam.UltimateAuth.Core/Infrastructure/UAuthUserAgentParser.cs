using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

internal sealed class UAuthUserAgentParser : IUserAgentParser
{
    public UserAgentInfo Parse(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return new UserAgentInfo();

        var ua = userAgent.ToLowerInvariant();

        return new UserAgentInfo
        {
            DeviceType = ResolveDeviceType(ua),
            Platform = ResolvePlatform(ua),
            OperatingSystem = ResolveOperatingSystem(ua),
            Browser = ResolveBrowser(ua)
        };
    }

    private static string ResolveDeviceType(string ua)
    {
        if (ua.Contains("ipad") || ua.Contains("tablet"))
            return "tablet";

        if (ua.Contains("mobi") || ua.Contains("iphone") || ua.Contains("android"))
            return "mobile";

        return "desktop";
    }

    private static string ResolvePlatform(string ua)
    {
        if (ua.Contains("ipad") || ua.Contains("tablet"))
            return "tablet";

        if (ua.Contains("mobi") || ua.Contains("iphone") || ua.Contains("android"))
            return "mobile";

        return "desktop";
    }

    private static string ResolveOperatingSystem(string ua)
    {
        if (ua.Contains("android"))
            return "android";

        if (ua.Contains("iphone") || ua.Contains("ipad"))
            return "ios";

        if (ua.Contains("windows"))
            return "windows";

        if (ua.Contains("mac"))
            return "macos";

        if (ua.Contains("linux"))
            return "linux";

        return "unknown";
    }

    private static string ResolveBrowser(string ua)
    {
        if (ua.Contains("edg/"))
            return "edge";

        if (ua.Contains("opr/") || ua.Contains("opera"))
            return "opera";

        if (ua.Contains("chrome") && !ua.Contains("chromium"))
            return "chrome";

        if (ua.Contains("safari") && !ua.Contains("chrome"))
            return "safari";

        if (ua.Contains("firefox"))
            return "firefox";

        return "unknown";
    }
}
