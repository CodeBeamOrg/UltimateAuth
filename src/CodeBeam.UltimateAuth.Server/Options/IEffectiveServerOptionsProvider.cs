using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Options
{
    public interface IEffectiveServerOptionsProvider
    {
        UAuthServerOptions Get(HttpContext context, AuthFlowType flowType);
    }
}
