using System;
using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server
{
    public class EffectiveAuthModeResolverTests
    {
        private readonly DefaultEffectiveAuthModeResolver _resolver = new();

        [Fact]
        public void ConfiguredMode_Wins_Over_ClientProfile()
        {
            var mode = _resolver.Resolve(
                configuredMode: UAuthMode.PureJwt,
                clientProfile: UAuthClientProfile.BlazorWasm,
                flowType: AuthFlowType.Login);

            Assert.Equal(UAuthMode.PureJwt, mode);
        }

        [Theory]
        [InlineData(UAuthClientProfile.BlazorServer, UAuthMode.PureOpaque)]
        [InlineData(UAuthClientProfile.BlazorWasm, UAuthMode.Hybrid)]
        [InlineData(UAuthClientProfile.Maui, UAuthMode.Hybrid)]
        [InlineData(UAuthClientProfile.Api, UAuthMode.PureJwt)]
        public void Default_Mode_Is_Derived_From_ClientProfile(UAuthClientProfile profile, UAuthMode expected)
        {
            var mode = _resolver.Resolve(
                configuredMode: null,
                clientProfile: profile,
                flowType: AuthFlowType.Login);

            Assert.Equal(expected, mode);
        }

    }
}
