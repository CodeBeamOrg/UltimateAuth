using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Options;
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
        private readonly IPrimaryTokenResolver _primaryTokenResolver;
        private readonly IEffectiveServerOptionsProvider _serverOptionsProvider;

        public DefaultAuthFlowContextFactory(
            IClientProfileReader clientProfileReader,
            IPrimaryTokenResolver primaryTokenResolver,
            IEffectiveServerOptionsProvider serverOptionsProvider)
        {
            _clientProfileReader = clientProfileReader;
            _primaryTokenResolver = primaryTokenResolver;
            _serverOptionsProvider = serverOptionsProvider;
        }

        public AuthFlowContext Create(HttpContext ctx, AuthFlowType flowType)
        {
            var clientProfile = _clientProfileReader.Read(ctx);
            var effectiveOptions = _serverOptionsProvider.Get(ctx, flowType);

            var primaryTokenKind =
                _primaryTokenResolver.Resolve(effectiveOptions.Mode);

            return new AuthFlowContext
            {
                ClientProfile = clientProfile,
                EffectiveMode = effectiveOptions.Mode,
                FlowType = flowType,
                PrimaryTokenKind = primaryTokenKind,
                ServerOptions = effectiveOptions
            };
        }

    }
}
