using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options
{
    internal sealed class DefaultEffectiveServerOptionsProvider : IEffectiveServerOptionsProvider
    {
        private readonly IOptions<UAuthServerOptions> _base;
        private readonly IClientProfileReader _clientProfileReader;
        private readonly IEffectiveAuthModeResolver _modeResolver;

        public DefaultEffectiveServerOptionsProvider(
            IOptions<UAuthServerOptions> baseOptions,
            IClientProfileReader clientProfileReader,
            IEffectiveAuthModeResolver modeResolver)
        {
            _base = baseOptions;
            _clientProfileReader = clientProfileReader;
            _modeResolver = modeResolver;
        }

        public UAuthServerOptions Get(HttpContext context, AuthFlowType flowType)
        {
            var baseOptions = _base.Value;
            var effective = baseOptions.Clone();
            var clientProfile = _clientProfileReader.Read(context);
            var mode = _modeResolver.Resolve(baseOptions.Mode, clientProfile, flowType);

            if (baseOptions.ModeConfigurations.TryGetValue(mode, out var configure))
            {
                configure(effective);
            }

            return effective;
        }

    }
}
