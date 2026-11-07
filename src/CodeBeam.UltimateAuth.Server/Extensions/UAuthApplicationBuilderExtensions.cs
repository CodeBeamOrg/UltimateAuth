using CodeBeam.UltimateAuth.Core.Runtime;
using CodeBeam.UltimateAuth.Server.Middlewares;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Extensions;

public static class UltimateAuthApplicationBuilderExtensions
{
    /// <summary>Installs UltimateAuth context middleware. Optional throttling requires routing first.</summary>
    /// <remarks>Do not combine helper-owned throttling with an independent UseRateLimiter call.</remarks>
    public static IApplicationBuilder UseUltimateAuth(this IApplicationBuilder app) =>
        UseUltimateAuth(app, useRateLimiting: false);

    public static IApplicationBuilder UseUltimateAuth(this IApplicationBuilder app, bool useRateLimiting)
    {
        app.UseUAuthExceptionHandling();
        app.UseMiddleware<TenantMiddleware>();
        app.UseMiddleware<SessionResolutionMiddleware>();
        app.UseMiddleware<UserMiddleware>();
        if (useRateLimiting) UseOwnedRateLimiter(app);

        return app;
    }

    // Both arguments are required to preserve positional bool calls as the original enableCors argument.
    /// <summary>
    /// Configures the UltimateAuth ASP.NET Core middleware pipeline, including CORS, authentication, rate limiting, and authorization.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="enableCors">
    /// Controls CORS middleware registration. When null, CORS is enabled automatically if required by UAuthHub.
    /// </param>
    /// <param name="useRateLimiting">
    /// Enables UltimateAuth's built-in rate limiting middleware. Defaults to true.
    /// </param>
    /// <returns>
    /// The application builder.
    /// </returns>
    public static IApplicationBuilder UseUltimateAuthWithAspNetCore(this IApplicationBuilder app, bool? enableCors = null, bool useRateLimiting = true)
    {
        var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger("UltimateAuth");

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

        if (useRateLimiting)
            UseOwnedRateLimiter(app);

        app.UseAuthorization();

        return app;
    }

    private const string OwnedRateLimiterKey = "CodeBeam.UltimateAuth.RateLimiterInstalled";

    private static void UseOwnedRateLimiter(IApplicationBuilder app)
    {
        if (app.Properties.TryGetValue(OwnedRateLimiterKey, out var owner) && ReferenceEquals(owner, app)) return;
        app.Properties[OwnedRateLimiterKey] = app;
        app.UseRateLimiter();
    }

    public static IApplicationBuilder UseUltimateAuthResourceApi(this IApplicationBuilder app)
    {
        var logger = app.ApplicationServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("UltimateAuth");

        var options = app.ApplicationServices.GetRequiredService<IOptions<UAuthResourceApiOptions>>().Value;

        if (options.AllowedClientOrigins?.Count > 0)
        {
            app.UseCors(options.CorsPolicyName);
            logger.LogInformation("UAuth Resource API initialized with CORS.");
        }

        app.UseUAuthExceptionHandling();
        app.UseMiddleware<TenantMiddleware>();
        app.UseMiddleware<SessionResolutionMiddleware>();
        app.UseMiddleware<RemoteSessionValidationMiddleware>();
        app.UseMiddleware<UserMiddleware>();

        return app;
    }

    public static IApplicationBuilder UseUltimateAuthResourceApiWithAspNetCore(this IApplicationBuilder app)
    {
        app.UseUltimateAuthResourceApi();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
