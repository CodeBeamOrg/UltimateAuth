using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Auth;

internal sealed class EffectiveAuthModeResolver : IEffectiveAuthModeResolver
{
    public UAuthMode Resolve(AuthModeResolutionContext context)
    {
        return context.ClientProfile switch
        {
            UAuthApplicationProfile.BlazorServer => UAuthMode.PureOpaque,
            UAuthApplicationProfile.WebServer => UAuthMode.Hybrid,
            UAuthApplicationProfile.BlazorWasm => UAuthMode.Hybrid,
            UAuthApplicationProfile.Maui => UAuthMode.Hybrid,
            UAuthApplicationProfile.Api => UAuthMode.PureJwt,
            UAuthApplicationProfile.ResourceApi => UAuthMode.PureJwt,
            _ => UAuthMode.Hybrid
        };
    }
}
