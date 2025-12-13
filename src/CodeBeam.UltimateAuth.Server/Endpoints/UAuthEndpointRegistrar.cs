using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CodeBeam.UltimateAuth.Server.Endpoints
{
    public interface IAuthEndpointRegistrar
    {
        void MapEndpoints(RouteGroupBuilder rootGroup, UAuthServerOptions options);
    }

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

                pkce.MapPost("/create", async (IPkceEndpointHandler h, HttpContext ctx)
                    => await h.CreateAsync(ctx));

                pkce.MapPost("/verify", async (IPkceEndpointHandler h, HttpContext ctx)
                    => await h.VerifyAsync(ctx));

                pkce.MapPost("/consume", async (IPkceEndpointHandler h, HttpContext ctx)
                    => await h.ConsumeAsync(ctx));
            }

            if (EndpointEnablement.Resolve(options.EnableLoginEndpoints, defaults.Login))
            {
                group.MapPost("/login", async (ILoginEndpointHandler h, HttpContext ctx)
                    => await h.LoginAsync(ctx));

                group.MapPost("/logout", async (ILogoutEndpointHandler h, HttpContext ctx)
                    => await h.LogoutAsync(ctx));

                group.MapPost("/refresh-session", async (ISessionRefreshEndpointHandler h, HttpContext ctx)
                    => await h.RefreshSessionAsync(ctx));

                group.MapPost("/reauth", async (IReauthEndpointHandler h, HttpContext ctx)
                    => await h.ReauthAsync(ctx));
            }

            if (EndpointEnablement.Resolve(options.EnableTokenEndpoints, defaults.Token))
            {
                var token = group.MapGroup("");

                token.MapPost("/token", async (ITokenEndpointHandler h, HttpContext ctx)
                    => await h.GetTokenAsync(ctx));

                token.MapPost("/refresh-token", async (ITokenEndpointHandler h, HttpContext ctx)
                    => await h.RefreshTokenAsync(ctx));

                token.MapPost("/introspect", async (ITokenEndpointHandler h, HttpContext ctx)
                    => await h.IntrospectAsync(ctx));

                token.MapPost("/revoke", async (ITokenEndpointHandler h, HttpContext ctx)
                    => await h.RevokeAsync(ctx));
            }

            if (EndpointEnablement.Resolve(options.EnableSessionEndpoints, defaults.Session))
            {
                var session = group.MapGroup("/session");

                session.MapGet("/current", async (ISessionManagementHandler h, HttpContext ctx)
                    => await h.GetCurrentSessionAsync(ctx));

                session.MapGet("/list", async (ISessionManagementHandler h, HttpContext ctx)
                    => await h.GetAllSessionsAsync(ctx));

                session.MapPost("/revoke/{sessionId}", async (ISessionManagementHandler h, string sessionId, HttpContext ctx)
                    => await h.RevokeSessionAsync(sessionId, ctx));

                session.MapPost("/revoke-all", async (ISessionManagementHandler h, HttpContext ctx)
                    => await h.RevokeAllAsync(ctx));
            }

            if (EndpointEnablement.Resolve(options.EnableUserInfoEndpoints, defaults.UserInfo))
            {
                var user = group.MapGroup("");

                user.MapGet("/userinfo", async (IUserInfoEndpointHandler h, HttpContext ctx)
                    => await h.GetUserInfoAsync(ctx));

                user.MapGet("/permissions", async (IUserInfoEndpointHandler h, HttpContext ctx)
                    => await h.GetPermissionsAsync(ctx));

                user.MapPost("/permissions/check", async (IUserInfoEndpointHandler h, HttpContext ctx)
                    => await h.CheckPermissionAsync(ctx));
            }
        }

    }
}
