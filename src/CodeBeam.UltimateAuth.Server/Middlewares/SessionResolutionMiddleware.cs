using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Server.Middlewares;

public sealed class SessionResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public SessionResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sessionIdResolver = context.RequestServices.GetRequiredService<ISessionIdResolver>();

        var tenant = context.GetTenant();
        var sessionId = sessionIdResolver.Resolve(context);

        var sessionContext = sessionId is null
            ? SessionContext.Anonymous()
            : SessionContext.FromSessionId(sessionId.Value, tenant);

        context.Items[SessionContextItemKeys.SessionContext] = sessionContext;

        await _next(context);
    }
}
