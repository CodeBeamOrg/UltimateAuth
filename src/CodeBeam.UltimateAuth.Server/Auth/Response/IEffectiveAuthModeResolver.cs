using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Auth;

public interface IEffectiveAuthModeResolver
{
    UAuthMode Resolve(UAuthClientProfile clientProfile, AuthFlowType flowType);
}
