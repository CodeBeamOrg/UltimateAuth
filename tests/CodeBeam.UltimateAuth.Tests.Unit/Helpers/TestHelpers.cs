using CodeBeam.UltimateAuth.Server;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

internal static class TestHelpers
{
    public static EffectiveServerOptionsProvider CreateEffectiveOptionsProvider(UAuthServerOptions options, IEffectiveAuthModeResolver? modeResolver = null)
    {
        return new EffectiveServerOptionsProvider(Options.Create(options), modeResolver ?? new EffectiveAuthModeResolver());
    }
}
