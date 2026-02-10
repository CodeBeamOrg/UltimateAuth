using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class ConfiguredClientBaseAddressProvider : IClientBaseAddressProvider
{
    public bool TryResolve(HttpContext context, UAuthServerOptions options, out string baseAddress)
    {
        baseAddress = default!;

        if (string.IsNullOrWhiteSpace(options.Hub.ClientBaseAddress))
            return false;

        baseAddress = options.Hub.ClientBaseAddress;
        return true;
    }
}
