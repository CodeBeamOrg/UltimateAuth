using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public interface IInnerSessionIdResolver
    {
        AuthSessionId? Resolve(HttpContext context);
    }
}
