using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit
{


    internal static class TestHelpers
    {
        public static DefaultEffectiveServerOptionsProvider CreateEffectiveOptionsProvider(UAuthServerOptions options)
        {
            return new DefaultEffectiveServerOptionsProvider(
                Options.Create(options),
                new DefaultClientProfileReader(),
                new DefaultEffectiveAuthModeResolver());
        }
    }
}
