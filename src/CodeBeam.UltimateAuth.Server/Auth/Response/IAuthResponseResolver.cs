using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Auth;

public interface IAuthResponseResolver
{
    EffectiveAuthResponse Resolve(UAuthMode effectiveMode, AuthFlowType flowType, UAuthClientProfile clientProfile, EffectiveUAuthServerOptions effectiveOptions);
}
