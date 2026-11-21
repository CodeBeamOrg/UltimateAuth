using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class EffectiveAuthModeResolverTests
{
    private readonly EffectiveAuthModeResolver _resolver = new();

    [Theory]
    [InlineData(UAuthApplicationProfile.BlazorServer, UAuthMode.PureOpaque)]
    [InlineData(UAuthApplicationProfile.BlazorWasm, UAuthMode.Hybrid)]
    [InlineData(UAuthApplicationProfile.Maui, UAuthMode.Hybrid)]
    [InlineData(UAuthApplicationProfile.Api, UAuthMode.PureJwt)]
    public void Default_Mode_Is_Derived_From_ApplicationProfile(UAuthApplicationProfile profile, UAuthMode expected)
    {
        var context = new AuthModeResolutionContext() { ApplicationProfile = profile, Tenant = TenantKey.Single };
        var mode = _resolver.Resolve(context);
        Assert.Equal(expected, mode);
    }
}
