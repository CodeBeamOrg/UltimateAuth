using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public interface ISessionIdResolver
    {
        AuthSessionId? Resolve(HttpContext context);
    }
}
