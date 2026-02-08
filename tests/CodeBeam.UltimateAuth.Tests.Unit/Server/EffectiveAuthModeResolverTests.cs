using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class EffectiveAuthModeResolverTests
{
    private readonly EffectiveAuthModeResolver _resolver = new();

    [Theory]
    [InlineData(UAuthClientProfile.BlazorServer, UAuthMode.PureOpaque)]
    [InlineData(UAuthClientProfile.BlazorWasm, UAuthMode.Hybrid)]
    [InlineData(UAuthClientProfile.Maui, UAuthMode.Hybrid)]
    [InlineData(UAuthClientProfile.Api, UAuthMode.PureJwt)]
    public void Default_Mode_Is_Derived_From_ClientProfile(UAuthClientProfile profile, UAuthMode expected)
    {
        var mode = _resolver.Resolve(clientProfile: profile, flowType: AuthFlowType.Login);
        Assert.Equal(expected, mode);
    }
}
