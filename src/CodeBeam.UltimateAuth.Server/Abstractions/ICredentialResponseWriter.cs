using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Abstractions
{
    public interface ICredentialResponseWriter
    {
        void Write(HttpContext context, string value, CredentialResponseOptions options);
    }
}
