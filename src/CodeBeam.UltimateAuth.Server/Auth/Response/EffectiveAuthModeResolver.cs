using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Auth;

internal sealed class EffectiveAuthModeResolver : IEffectiveAuthModeResolver
{
    public UAuthMode Resolve(UAuthMode? configuredMode, UAuthClientProfile clientProfile, AuthFlowType flowType)
    {
        if (configuredMode.HasValue)
            return configuredMode.Value;

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
