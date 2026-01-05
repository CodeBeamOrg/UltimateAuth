using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    internal sealed class DefaultAuthFlow : IAuthFlow
    {
        private readonly IHttpContextAccessor _http;
        private readonly IAuthFlowContextFactory _factory;
        private readonly DefaultAuthFlowContextAccessor _accessor;

        public DefaultAuthFlow(
            IHttpContextAccessor http,
            IAuthFlowContextFactory factory,
            IAuthFlowContextAccessor accessor)
        {
            _http = http;
            _factory = factory;
            _accessor = (DefaultAuthFlowContextAccessor)accessor;
        }

        public AuthFlowContext Begin(AuthFlowType flowType)
        {
            var ctx = _http.HttpContext ?? throw new InvalidOperationException("No HttpContext.");

            var flowContext = _factory.Create(ctx, flowType);
            _accessor.Set(flowContext);

            return flowContext;
        }

    }
}
