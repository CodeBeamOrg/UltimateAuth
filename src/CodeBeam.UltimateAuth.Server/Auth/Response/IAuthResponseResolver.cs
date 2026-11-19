using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Auth;

public interface IAuthResponseResolver
{
    EffectiveAuthResponse Resolve(UAuthMode effectiveMode, AuthFlowType flowType, UAuthApplicationProfile clientProfile, EffectiveUAuthServerOptions effectiveOptions);
    EffectiveAuthResponse ResolveForAuthentication(UAuthMode effectiveMode, UAuthApplicationProfile clientProfile, EffectiveUAuthServerOptions effectiveOptions);
}
