using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    public interface IAuthFlowContextFactory
    {
        AuthFlowContext Create(HttpContext httpContext, AuthFlowType flowType);
    }

    public sealed class DefaultAuthFlowContextFactory : IAuthFlowContextFactory
    {
        private readonly IClientProfileReader _clientProfileReader;
        private readonly IEffectiveAuthModeResolver _modeResolver;
        private readonly IPrimaryTokenResolver _primaryTokenResolver;
        private readonly IEffectiveServerOptionsResolver _serverOptionsResolver;

        public DefaultAuthFlowContextFactory(
            IClientProfileReader clientProfileReader,
            IEffectiveAuthModeResolver modeResolver,
            IPrimaryTokenResolver primaryTokenResolver,
            IEffectiveServerOptionsResolver serverOptionsResolver)
        {
            _clientProfileReader = clientProfileReader;
            _modeResolver = modeResolver;
            _primaryTokenResolver = primaryTokenResolver;
            _serverOptionsResolver = serverOptionsResolver;
        }

        public AuthFlowContext Create(HttpContext httpContext, AuthFlowType flowType)
        {
            var clientProfile = _clientProfileReader.Read(httpContext);
            var configuredMode = _serverOptionsResolver.GetConfiguredMode();
            var effectiveMode = _modeResolver.Resolve(configuredMode, clientProfile, flowType);
            var primaryTokenKind = _primaryTokenResolver.Resolve(effectiveMode);
            var effectiveOptions = _serverOptionsResolver.Resolve(effectiveMode);

            return new AuthFlowContext
            {
                ClientProfile = clientProfile,
                EffectiveMode = effectiveMode,
                FlowType = flowType,
                PrimaryTokenKind = primaryTokenKind,
                ServerOptions = effectiveOptions
            };
        }

    }
}
