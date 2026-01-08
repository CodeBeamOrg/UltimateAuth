using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    public interface IAuthFlowContextFactory
    {
        AuthFlowContext Create(HttpContext httpContext, AuthFlowType flowType);
    }

    internal sealed class DefaultAuthFlowContextFactory : IAuthFlowContextFactory
    {
        private readonly IClientProfileReader _clientProfileReader;
        private readonly IPrimaryTokenResolver _primaryTokenResolver;
        private readonly IEffectiveServerOptionsProvider _serverOptionsProvider;
        private readonly IAuthResponseResolver _authResponseResolver;

        public DefaultAuthFlowContextFactory(
            IClientProfileReader clientProfileReader,
            IPrimaryTokenResolver primaryTokenResolver,
            IEffectiveServerOptionsProvider serverOptionsProvider,
            IAuthResponseResolver authResponseResolver)
        {
            _clientProfileReader = clientProfileReader;
            _primaryTokenResolver = primaryTokenResolver;
            _serverOptionsProvider = serverOptionsProvider;
            _authResponseResolver = authResponseResolver;
        }

        public AuthFlowContext Create(HttpContext ctx, AuthFlowType flowType)
        {
            var tenant = ctx.GetTenantContext();
            var session = ctx.GetSessionContext();
            var user = ctx.GetUserContext();

            var clientProfile = _clientProfileReader.Read(ctx);
            var originalOptions = _serverOptionsProvider.GetOriginal(ctx);
            var effectiveOptions = _serverOptionsProvider.GetEffective(ctx, flowType, clientProfile);

            var effectiveMode = effectiveOptions.Mode;
            var primaryTokenKind = _primaryTokenResolver.Resolve(effectiveMode);

            var response = _authResponseResolver.Resolve(effectiveMode, flowType, clientProfile, effectiveOptions);

            // TODO: Implement invariant checker
            //_invariantChecker.Validate(flowType, effectiveMode, response, effectiveOptions);

            return new AuthFlowContext(
                flowType,
                clientProfile,
                effectiveMode,
                tenant?.TenantId,
                user?.IsAuthenticated ?? false,
                user?.UserId,
                session?.SessionId,
                originalOptions,
                effectiveOptions,
                response,
                primaryTokenKind
            );
        }

    }
}
