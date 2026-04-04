using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

internal static class TestServerOptions
{
    public static UAuthServerOptions Default()
        => new()
        {
            Hub =
            {
            ClientBaseAddress = "https://app.example.com"
            }
        };

    public static EffectiveUAuthServerOptions Effective(UAuthMode mode = UAuthMode.PureOpaque)
        => new EffectiveUAuthServerOptions
        {
            Mode = mode,
            Options = Default()
        };
}
