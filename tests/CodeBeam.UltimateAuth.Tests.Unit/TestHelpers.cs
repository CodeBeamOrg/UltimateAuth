using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit;

internal static class TestHelpers
{
    public static EffectiveServerOptionsProvider CreateEffectiveOptionsProvider(UAuthServerOptions options, IEffectiveAuthModeResolver? modeResolver = null)
    {
        return new EffectiveServerOptionsProvider(Options.Create(options), modeResolver ?? new EffectiveAuthModeResolver());
    }
}
