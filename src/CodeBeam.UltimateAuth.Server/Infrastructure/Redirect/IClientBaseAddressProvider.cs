using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal interface IClientBaseAddressProvider
{
    bool TryResolve(HttpContext context, UAuthServerOptions options, out string baseAddress);
}
