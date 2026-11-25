using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Auth;

internal sealed class EffectiveAuthModeResolver : IEffectiveAuthModeResolver
{
    public UAuthMode Resolve(AuthModeResolutionContext context)
    {
        return context.ApplicationProfile switch
        {
            UAuthApplicationProfile.BlazorServer => UAuthMode.PureOpaque,
            UAuthApplicationProfile.WebServer => UAuthMode.Hybrid,
            UAuthApplicationProfile.BlazorWasm => UAuthMode.Hybrid,
            UAuthApplicationProfile.Maui => UAuthMode.Hybrid,
            UAuthApplicationProfile.UAuthHub => UAuthMode.Hybrid,
            UAuthApplicationProfile.Api => UAuthMode.Hybrid,
            UAuthApplicationProfile.ResourceApi => UAuthMode.Hybrid,
            _ => UAuthMode.Hybrid
        };
    }
}
