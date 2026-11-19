using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public class AuthResponseResolverTests
{
    [Theory]
    [InlineData(UAuthMode.PureOpaque, true, false, false)]
    [InlineData(UAuthMode.Hybrid, true, true, true)]
    [InlineData(UAuthMode.SemiHybrid, false, true, true)]
    [InlineData(UAuthMode.PureJwt, false, true, true)]
    public void Resolve_ShouldEnableExpectedGrants(UAuthMode mode, bool session, bool accessToken, bool refreshToken)
    {
        var sut = new AuthResponseResolver();

        var effective = new EffectiveUAuthServerOptions
        {
            Mode = mode,
            Options = new UAuthServerOptions()
        };

        var result = sut.ResolveForAuthentication(mode, UAuthApplicationProfile.BlazorWasm, effective);

        Assert.Equal(session, result.SessionIdDelivery.Mode != GrantDeliveryMode.None);
        Assert.Equal(accessToken, result.AccessTokenDelivery.Mode != GrantDeliveryMode.None);
        Assert.Equal(refreshToken, result.RefreshTokenDelivery.Mode != GrantDeliveryMode.None);
    }
}
