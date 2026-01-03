using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    internal sealed class DefaultEffectiveServerOptionsResolver : IEffectiveServerOptionsResolver
    {
        private readonly IOptions<UAuthServerOptions> _base;

        public DefaultEffectiveServerOptionsResolver(IOptions<UAuthServerOptions> baseOptions)
        {
            _base = baseOptions;
        }

        public UAuthMode? GetConfiguredMode()
        {
            return _base.Value.Mode;
        }

        public EffectiveUAuthServerOptions Resolve(UAuthMode mode)
        {
            var options = _base.Value.Clone();
            options.Mode = mode;

            ConfigureDefaults.ApplyModeDefaults(options);

            return new EffectiveUAuthServerOptions
            {
                Mode = mode,
                Options = options
            };
        }
    }
}
