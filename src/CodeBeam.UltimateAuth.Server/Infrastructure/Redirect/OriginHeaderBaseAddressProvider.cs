using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class OriginHeaderBaseAddressProvider : IClientBaseAddressProvider
{
    public bool TryResolve(HttpContext context, UAuthServerOptions options, out string baseAddress)
    {
        baseAddress = default!;

        if (!context.Request.Headers.TryGetValue("Origin", out var origin))
            return false;

        if (!Uri.TryCreate(origin!, UriKind.Absolute, out var uri))
            return false;

        baseAddress = uri.GetLeftPart(UriPartial.Authority);
        return true;
    }
}
