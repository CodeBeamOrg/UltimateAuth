using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class RequestHostBaseAddressProvider : IClientBaseAddressProvider, IFallbackClientBaseAddressProvider
{
    public bool TryResolve(HttpContext context, UAuthServerOptions options, out string baseAddress)
    {
        baseAddress = $"{context.Request.Scheme}://{context.Request.Host}";
        return true;
    }
}
