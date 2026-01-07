using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server
{
    public class EffectiveServerOptionsProviderTests
    {
        [Fact]
        public void Original_Options_Are_Not_Mutated()
        {
            var baseOptions = new UAuthServerOptions
            {
                Mode = UAuthMode.Hybrid
            };

            var provider = TestHelpers.CreateEffectiveOptionsProvider(baseOptions);
            var ctx = new DefaultHttpContext();

            var effective = provider.GetEffective(
                ctx,
                AuthFlowType.Login,
                UAuthClientProfile.BlazorServer);

            effective.Options.Tokens.AccessTokenLifetime = TimeSpan.FromSeconds(10);

            Assert.NotEqual(
                baseOptions.Tokens.AccessTokenLifetime,
                effective.Options.Tokens.AccessTokenLifetime
            );
        }


        [Fact]
        public void EffectiveMode_Comes_From_ModeResolver()
        {
            var baseOptions = new UAuthServerOptions
            {
                Mode = null // Not specified
            };

            var provider = TestHelpers.CreateEffectiveOptionsProvider(baseOptions);
            var ctx = new DefaultHttpContext();

            var effective = provider.GetEffective(
                ctx,
                AuthFlowType.Login,
                UAuthClientProfile.Api);

            Assert.Equal(UAuthMode.PureJwt, effective.Mode);
        }

        [Fact]
        public void Mode_Defaults_Are_Applied()
        {
            var baseOptions = new UAuthServerOptions
            {
                Mode = UAuthMode.PureOpaque
            };

            var provider = TestHelpers.CreateEffectiveOptionsProvider(baseOptions);
            var ctx = new DefaultHttpContext();

            var effective = provider.GetEffective(
                ctx,
                AuthFlowType.Login,
                UAuthClientProfile.BlazorServer);

            Assert.True(effective.Options.Session.SlidingExpiration);
            Assert.NotNull(effective.Options.Session.IdleTimeout);
        }

        [Fact]
        public void ModeConfiguration_Overrides_Defaults()
        {
            var baseOptions = new UAuthServerOptions
            {
                Mode = UAuthMode.Hybrid
            };

            baseOptions.ConfigureMode(UAuthMode.Hybrid, o =>
            {
                o.Tokens.AccessTokenLifetime = TimeSpan.FromMinutes(1);
            });

            var provider = TestHelpers.CreateEffectiveOptionsProvider(baseOptions);
            var ctx = new DefaultHttpContext();

            var effective = provider.GetEffective(
                ctx,
                AuthFlowType.Login,
                UAuthClientProfile.BlazorServer);

            Assert.Equal(
                TimeSpan.FromMinutes(1),
                effective.Options.Tokens.AccessTokenLifetime
            );
        }

        [Fact]
        public void Each_Call_Returns_New_EffectiveOptions_Instance()
        {
            var baseOptions = new UAuthServerOptions
            {
                Mode = UAuthMode.Hybrid
            };

            var provider = TestHelpers.CreateEffectiveOptionsProvider(baseOptions);
            var ctx = new DefaultHttpContext();

            var first = provider.GetEffective(ctx, AuthFlowType.Login, UAuthClientProfile.BlazorServer);
            var second = provider.GetEffective(ctx, AuthFlowType.Login, UAuthClientProfile.BlazorServer);

            Assert.NotSame(first.Options, second.Options);
        }

        // TODO: Discuss and enable
        //[Fact]
        //public void FlowType_Is_Passed_To_ModeResolver()
        //{
        //    var baseOptions = new UAuthServerOptions
        //    {
        //        Mode = null
        //    };

        //    var provider = TestHelpers.CreateEffectiveOptionsProvider(baseOptions);
        //    var ctx = new DefaultHttpContext();

        //    var login = provider.GetEffective(
        //        ctx,
        //        AuthFlowType.Login,
        //        UAuthClientProfile.Api);

        //    var api = provider.GetEffective(
        //        ctx,
        //        AuthFlowType.ApiAccess,
        //        UAuthClientProfile.Api);

        //    Assert.NotEqual(login.Mode, api.Mode);
        //}

    }
}
