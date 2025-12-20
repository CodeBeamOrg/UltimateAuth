using CodeBeam.UltimateAuth.Server.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace CodeBeam.UltimateAuth.Server.Extensions
{
    public static class UltimateAuthApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseUltimateAuthServer(this IApplicationBuilder app)
        {
            app.UseMiddleware<TenantMiddleware>();
            app.UseMiddleware<SessionResolutionMiddleware>();
            app.UseMiddleware<UserMiddleware>();

            return app;
        }
    }

}
