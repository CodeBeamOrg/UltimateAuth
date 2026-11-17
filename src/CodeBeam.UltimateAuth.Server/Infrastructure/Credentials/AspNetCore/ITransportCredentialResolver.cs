using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Server.Auth;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public interface ITransportCredentialResolver
{
    ValueTask<TransportCredential?> ResolveAsync(HttpContext context, UAuthMode mode, EffectiveAuthResponse response);
}
