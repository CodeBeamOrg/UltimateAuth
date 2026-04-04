using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public interface ITransportCredentialResolver
{
    ValueTask<TransportCredential?> ResolveAsync(HttpContext context);
}
