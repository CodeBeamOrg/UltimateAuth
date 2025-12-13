using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Sessions
{
    public interface ISessionIdResolver
    {
        AuthSessionId? Resolve(HttpContext context);
    }
}
