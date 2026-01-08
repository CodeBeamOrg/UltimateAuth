using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public interface ITransportCredentialResolver
    {
        TransportCredential? Resolve(HttpContext context);
    }
}
