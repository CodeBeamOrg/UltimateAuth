using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options
{
    internal sealed class DefaultEffectiveServerOptionsProvider : IEffectiveServerOptionsProvider
    {
        private readonly IOptions<UAuthServerOptions> _baseOptions;
        private readonly IEffectiveAuthModeResolver _modeResolver;

        public DefaultEffectiveServerOptionsProvider(IOptions<UAuthServerOptions> baseOptions, IEffectiveAuthModeResolver modeResolver)
        {
            _baseOptions = baseOptions;
            _modeResolver = modeResolver;
        }

        public UAuthServerOptions GetOriginal(HttpContext context)
        {
            return _baseOptions.Value;
        }

        public EffectiveUAuthServerOptions GetEffective(HttpContext context, AuthFlowType flowType, UAuthClientProfile clientProfile)
        {
            var original = _baseOptions.Value;
            var effectiveMode = _modeResolver.Resolve(original.Mode, clientProfile, flowType);
            var options = original.Clone();
            options.Mode = effectiveMode;

            ConfigureDefaults.ApplyModeDefaults(options);

            if (original.ModeConfigurations.TryGetValue(effectiveMode, out var configure))
            {
                configure(options);
            }

            return new EffectiveUAuthServerOptions
            {
                Mode = effectiveMode,
                Options = options
            };
        }

    }
}
