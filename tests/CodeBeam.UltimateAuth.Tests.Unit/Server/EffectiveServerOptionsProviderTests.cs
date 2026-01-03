using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server
{
    public class EffectiveServerOptionsProviderTests
    {
        [Fact]
        public void Clone_Isolated_From_Global_Config()
        {
            var baseOptions = new UAuthServerOptions
            {
                Mode = UAuthMode.Hybrid
            };

            var provider = TestHelpers.CreateEffectiveOptionsProvider(baseOptions);

            var ctx = new DefaultHttpContext();

            var effective = provider.Get(ctx, AuthFlowType.Login);

            effective.Tokens.AccessTokenLifetime = TimeSpan.FromSeconds(30);

            Assert.NotEqual(baseOptions.Tokens.AccessTokenLifetime, effective.Tokens.AccessTokenLifetime);
        }

        [Fact]
        public void Mode_Specific_Override_Is_Applied()
        {
            var baseOptions = new UAuthServerOptions
            {
                Mode = UAuthMode.PureJwt
            };

            baseOptions.ConfigureMode(UAuthMode.PureJwt, o =>
            {
                o.Tokens.AccessTokenLifetime = TimeSpan.FromMinutes(1);
            });

            var provider = TestHelpers.CreateEffectiveOptionsProvider(baseOptions);
            var ctx = new DefaultHttpContext();

            var effective = provider.Get(ctx, AuthFlowType.Login);

            Assert.Equal(TimeSpan.FromMinutes(1), effective.Tokens.AccessTokenLifetime);
        }
    }
}
