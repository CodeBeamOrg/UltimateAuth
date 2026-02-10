using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class RefererHeaderBaseAddressProvider : IClientBaseAddressProvider
{
    public bool TryResolve(HttpContext context, UAuthServerOptions options, out string baseAddress)
    {
        baseAddress = default!;

        if (!context.Request.Headers.TryGetValue("Referer", out var referer))
            return false;

        if (!Uri.TryCreate(referer!, UriKind.Absolute, out var uri))
            return false;

        baseAddress = uri.GetLeftPart(UriPartial.Authority);
        return true;
    }
}
