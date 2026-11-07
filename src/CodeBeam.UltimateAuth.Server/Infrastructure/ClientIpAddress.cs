using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

// Forwarded headers are processed by the host, with its explicit proxy trust configuration.
internal static class ClientIpAddress
{
    internal static string Resolve(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress;
        if (ip?.IsIPv4MappedToIPv6 == true) ip = ip.MapToIPv4();
        return ip?.ToString() ?? "unknown";
    }
}
