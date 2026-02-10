using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

internal sealed class TestAuthModeResolver : IEffectiveAuthModeResolver
{
    public UAuthMode Resolve(UAuthClientProfile profile, AuthFlowType flowType)
        => UAuthMode.PureOpaque;
}
