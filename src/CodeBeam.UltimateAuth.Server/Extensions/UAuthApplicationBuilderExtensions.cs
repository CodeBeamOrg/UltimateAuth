using CodeBeam.UltimateAuth.Core.Runtime;
using CodeBeam.UltimateAuth.Server.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeBeam.UltimateAuth.Server.Extensions;

public static class UltimateAuthApplicationBuilderExtensions
{
    public static IApplicationBuilder UseUltimateAuth(this IApplicationBuilder app)
    {
        app.UseUAuthExceptionHandling();
        app.UseMiddleware<TenantMiddleware>();
        app.UseMiddleware<SessionResolutionMiddleware>();
        app.UseMiddleware<UserMiddleware>();

        return app;
    }

    public static IApplicationBuilder UseUltimateAuthWithAspNetCore(this IApplicationBuilder app, bool? enableCors = null)
    {
        var logger = app.ApplicationServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("UltimateAuth");

        var marker = app.ApplicationServices.GetService<IUAuthHubMarker>();
        var requiresCors = marker?.RequiresCors == true;

        if (enableCors == true || (enableCors == null && requiresCors))
            app.UseCors();

        if (requiresCors && enableCors == false)
        {
            logger.LogWarning("UAuthHub requires CORS. Either call app.UseCors() or enable it via UseUltimateAuthWithAspNetCore(enableCors: true).");
        }

        app.UseUltimateAuth();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
