using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Options
{
    public interface IEffectiveServerOptionsProvider
    {
        EffectiveUAuthServerOptions Get(HttpContext context, AuthFlowType flowType);
    }
}
