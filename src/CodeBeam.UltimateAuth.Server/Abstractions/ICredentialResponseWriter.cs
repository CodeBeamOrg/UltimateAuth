using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Abstractions
{
    public interface ICredentialResponseWriter
    {
        void Write(HttpContext context, CredentialKind kind, string value);
    }
}
