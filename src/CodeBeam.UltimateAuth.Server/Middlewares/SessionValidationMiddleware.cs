using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Middlewares;

public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ISessionValidator _validator;
    private readonly IClock _clock;

    public SessionValidationMiddleware(RequestDelegate next, ISessionValidator validator, IClock clock)
    {
        _next = next;
        _validator = validator;
        _clock = clock;
    }

    public async Task Invoke(HttpContext context)
    {
        var sessionCtx = context.GetSessionContext();

        if (sessionCtx.IsAnonymous)
        {
            context.Items[UAuthConstants.HttpItems.SessionValidationResult] = SessionValidationResult.Invalid(SessionState.NotFound);

            await _next(context);
            return;
        }

        var info = await context.GetDeviceAsync();
        var device = DeviceContext.Create(info.DeviceId, info.DeviceType, info.Platform, info.OperatingSystem, info.Browser, info.IpAddress);

        if (sessionCtx.Tenant is not TenantKey tenant)
            throw new InvalidOperationException("Tenant is not resolved.");

        var result = await _validator.ValidateSessionAsync(new SessionValidationContext
        {
            Tenant = tenant,
            SessionId = sessionCtx.SessionId!.Value,
            Now = _clock.UtcNow,
            Device = device
        });

        context.Items["__UAuth.SessionValidationResult"] = result;

        await _next(context);
    }
}
