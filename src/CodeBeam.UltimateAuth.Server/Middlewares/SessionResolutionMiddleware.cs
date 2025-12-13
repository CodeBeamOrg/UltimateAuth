using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Sessions;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Middlewares
{
    public sealed class SessionResolutionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ISessionIdResolver _sessionIdResolver;

        public const string SessionContextKey = "__UAuthSession";

        public SessionResolutionMiddleware(
            RequestDelegate next,
            ISessionIdResolver sessionIdResolver)
        {
            _next = next;
            _sessionIdResolver = sessionIdResolver;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var tenant = context.GetTenantContext();
            var sessionId = _sessionIdResolver.Resolve(context);

            var sessionContext = sessionId is null
                ? SessionContext.Anonymous()
                : SessionContext.FromSessionId(
                    sessionId.Value,
                    tenant.TenantId);

            context.Items[SessionContextKey] = sessionContext;

            await _next(context);
        }
    }
}
