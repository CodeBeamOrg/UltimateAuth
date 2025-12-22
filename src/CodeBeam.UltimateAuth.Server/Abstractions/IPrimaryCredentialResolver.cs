using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Abstractions
{
    public interface IPrimaryCredentialResolver
    {
        PrimaryCredentialKind Resolve(HttpContext context);
    }
}
