using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Server.Middlewares
{
    public sealed class SessionResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public const string SessionContextKey = "__UAuthSession";

        public SessionResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sessionIdResolver = context.RequestServices.GetRequiredService<ISessionIdResolver>();

            var tenant = context.GetTenantContext();
            var sessionId = sessionIdResolver.Resolve(context);

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
