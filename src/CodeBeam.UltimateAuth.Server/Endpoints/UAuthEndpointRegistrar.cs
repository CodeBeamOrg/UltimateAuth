using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public interface IAuthEndpointRegistrar
    {
        void MapEndpoints(RouteGroupBuilder rootGroup, UAuthServerOptions options);
    }

    // TODO: Add Scalar/Swagger integration
    public class UAuthEndpointRegistrar : IAuthEndpointRegistrar
    {
        public void MapEndpoints(RouteGroupBuilder rootGroup, UAuthServerOptions options)
        {
            var defaults = UAuthEndpointDefaultsMap.ForMode(options.Mode);

            // Base: /auth
            string basePrefix = options.RoutePrefix.TrimStart('/');

            bool useRouteTenant =
                options.MultiTenant.Enabled &&
                options.MultiTenant.EnableRoute;

            RouteGroupBuilder group = useRouteTenant
                ? rootGroup.MapGroup("/{tenant}/" + basePrefix)
                : rootGroup.MapGroup("/" + basePrefix);

            if (EndpointEnablement.Resolve(options.EnablePkceEndpoints, defaults.Pkce))
            {
                var pkce = group.MapGroup("/pkce");

                pkce.MapPost("/create",
                    async ([FromServices] IPkceEndpointHandler h, HttpContext ctx)
                        => await h.CreateAsync(ctx));

                pkce.MapPost("/verify",
                    async ([FromServices] IPkceEndpointHandler h, HttpContext ctx)
                        => await h.VerifyAsync(ctx));

                pkce.MapPost("/consume",
                    async ([FromServices] IPkceEndpointHandler h, HttpContext ctx)
                        => await h.ConsumeAsync(ctx));
            }

            if (EndpointEnablement.Resolve(options.EnableLoginEndpoints, defaults.Login))
            {
                group.MapPost("/login", async ([FromServices] ILoginEndpointHandler h, HttpContext ctx)
                    => await h.LoginAsync(ctx));

                group.MapPost("/logout", async ([FromServices] ILogoutEndpointHandler h, HttpContext ctx)
                    => await h.LogoutAsync(ctx));

                group.MapPost("/refresh-session", async ([FromServices] ISessionRefreshEndpointHandler h, HttpContext ctx)
                    => await h.RefreshSessionAsync(ctx));

                group.MapPost("/reauth", async ([FromServices] IReauthEndpointHandler h, HttpContext ctx)
                    => await h.ReauthAsync(ctx));
            }

            if (EndpointEnablement.Resolve(options.EnableTokenEndpoints, defaults.Token))
            {
                var token = group.MapGroup("");

                token.MapPost("/token", async ([FromServices] ITokenEndpointHandler h, HttpContext ctx)
                    => await h.GetTokenAsync(ctx));

                token.MapPost("/refresh-token", async ([FromServices] ITokenEndpointHandler h, HttpContext ctx)
                    => await h.RefreshTokenAsync(ctx));

                token.MapPost("/introspect", async ([FromServices] ITokenEndpointHandler h, HttpContext ctx)
                    => await h.IntrospectAsync(ctx));

                token.MapPost("/revoke", async ([FromServices] ITokenEndpointHandler h, HttpContext ctx)
                    => await h.RevokeAsync(ctx));
            }

            if (EndpointEnablement.Resolve(options.EnableSessionEndpoints, defaults.Session))
            {
                var session = group.MapGroup("/session");

                session.MapGet("/current", async ([FromServices] ISessionManagementHandler h, HttpContext ctx)
                    => await h.GetCurrentSessionAsync(ctx));

                session.MapGet("/list", async ([FromServices] ISessionManagementHandler h, HttpContext ctx)
                    => await h.GetAllSessionsAsync(ctx));

                session.MapPost("/revoke/{sessionId}", async ([FromServices] ISessionManagementHandler h, string sessionId, HttpContext ctx)
                    => await h.RevokeSessionAsync(sessionId, ctx));

                session.MapPost("/revoke-all", async ([FromServices] ISessionManagementHandler h, HttpContext ctx)
                    => await h.RevokeAllAsync(ctx));
            }

            if (EndpointEnablement.Resolve(options.EnableUserInfoEndpoints, defaults.UserInfo))
            {
                var user = group.MapGroup("");

                user.MapGet("/userinfo", async ([FromServices] IUserInfoEndpointHandler h, HttpContext ctx)
                    => await h.GetUserInfoAsync(ctx));

                user.MapGet("/permissions", async ([FromServices] IUserInfoEndpointHandler h, HttpContext ctx)
                    => await h.GetPermissionsAsync(ctx));

                user.MapPost("/permissions/check", async ([FromServices] IUserInfoEndpointHandler h, HttpContext ctx)
                    => await h.CheckPermissionAsync(ctx));
            }
        }

    }
}
