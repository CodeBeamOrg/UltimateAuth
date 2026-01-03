using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Extensions
{
    public static class UAuthServerOptionsExtensions
    {
        public static void ConfigureMode(this UAuthServerOptions options, UAuthMode mode, Action<UAuthServerOptions> configure)
        {
            options.ModeConfigurations[mode] = configure;
        }
    }
}
