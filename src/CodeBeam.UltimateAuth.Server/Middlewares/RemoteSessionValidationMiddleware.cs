using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Server.Middlewares;

public class RemoteSessionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IClock _clock;

    public RemoteSessionValidationMiddleware(RequestDelegate next, IClock clock)
    {
        _next = next;
        _clock = clock;
    }

    public async Task InvokeAsync(HttpContext context, ISessionValidator validator)
    {
        var sessionCtx = context.GetSessionContext();

        if (sessionCtx.IsAnonymous)
        {
            context.Items[UAuthConstants.HttpItems.SessionValidationResult] = SessionValidationResult.Invalid(SessionState.NotFound);

            await _next(context);
            return;
        }

        if (sessionCtx.Tenant is not TenantKey tenant)
            throw new InvalidOperationException("Tenant is not resolved.");

        if (sessionCtx.SessionId is not AuthSessionId sessionId)
            throw new InvalidOperationException("Session ID is not resolved.");

        var info = await context.GetDeviceAsync();

        var device = DeviceContext.Create(
            info.DeviceId,
            info.DeviceType,
            info.Platform,
            info.OperatingSystem,
            info.Browser,
            info.IpAddress);

        var result = await validator.ValidateSessionAsync(
            new SessionValidationContext
            {
                Tenant = tenant,
                SessionId = sessionId,
                Now = _clock.UtcNow,
                Device = device
            },
            context.RequestAborted);

        context.Items[UAuthConstants.HttpItems.SessionValidationResult] = result;

        await _next(context);
    }
}
