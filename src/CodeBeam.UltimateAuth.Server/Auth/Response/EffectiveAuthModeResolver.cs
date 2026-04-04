using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Auth;

internal sealed class EffectiveAuthModeResolver : IEffectiveAuthModeResolver
{
    public UAuthMode Resolve(UAuthClientProfile clientProfile, AuthFlowType flowType)
    {
        return clientProfile switch
        {
            UAuthClientProfile.BlazorServer => UAuthMode.PureOpaque,
            UAuthClientProfile.WebServer => UAuthMode.Hybrid,
            UAuthClientProfile.BlazorWasm => UAuthMode.Hybrid,
            UAuthClientProfile.Maui => UAuthMode.Hybrid,
            UAuthClientProfile.Api => UAuthMode.PureJwt,
            _ => UAuthMode.Hybrid
        };
    }
}
