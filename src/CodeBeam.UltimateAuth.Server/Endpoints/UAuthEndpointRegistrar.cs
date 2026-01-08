using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
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
    // TODO: Add endpoint based guards
    public class UAuthEndpointRegistrar : IAuthEndpointRegistrar
    {
        public void MapEndpoints(RouteGroupBuilder rootGroup, UAuthServerOptions options)
        {
            // Base: /auth
            string basePrefix = options.RoutePrefix.TrimStart('/');

            bool useRouteTenant =
                options.MultiTenant.Enabled &&
                options.MultiTenant.EnableRoute;

            RouteGroupBuilder group = useRouteTenant
                ? rootGroup.MapGroup("/{tenant}/" + basePrefix)
                : rootGroup.MapGroup("/" + basePrefix);

            group.AddEndpointFilter<AuthFlowEndpointFilter>();

            if (options.EnablePkceEndpoints != false)
            {
                var pkce = group.MapGroup("/pkce");

                pkce.MapPost("/create",
                    async ([FromServices] IPkceEndpointHandler h, HttpContext ctx)
                        => await h.CreateAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Login));

                pkce.MapPost("/verify",
                    async ([FromServices] IPkceEndpointHandler h, HttpContext ctx)
                        => await h.VerifyAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Login));

                pkce.MapPost("/consume",
                    async ([FromServices] IPkceEndpointHandler h, HttpContext ctx)
                        => await h.ConsumeAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Login));
            }

            if (options.EnableLoginEndpoints != false)
            {
                group.MapPost("/login", async ([FromServices] ILoginEndpointHandler h, HttpContext ctx)
                    => await h.LoginAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Login));

                group.MapPost("/validate", async ([FromServices] IValidateEndpointHandler h, HttpContext ctx)
                    => await h.ValidateAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.ValidateSession));

                group.MapPost("/logout", async ([FromServices] ILogoutEndpointHandler h, HttpContext ctx)
                    => await h.LogoutAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Logout));

                group.MapPost("/refresh", async ([FromServices] IRefreshEndpointHandler h, HttpContext ctx)
                    => await h.RefreshAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RefreshSession));

                group.MapPost("/reauth", async ([FromServices] IReauthEndpointHandler h, HttpContext ctx)
                    => await h.ReauthAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Reauthentication));
            }

            if (options.EnableTokenEndpoints != false)
            {
                var token = group.MapGroup("");

                token.MapPost("/token", async ([FromServices] ITokenEndpointHandler h, HttpContext ctx)
                    => await h.GetTokenAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.IssueToken));

                token.MapPost("/refresh-token", async ([FromServices] ITokenEndpointHandler h, HttpContext ctx)
                    => await h.RefreshTokenAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RefreshToken));

                token.MapPost("/introspect", async ([FromServices] ITokenEndpointHandler h, HttpContext ctx)
                    => await h.IntrospectAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.IntrospectToken));

                token.MapPost("/revoke", async ([FromServices] ITokenEndpointHandler h, HttpContext ctx)
                    => await h.RevokeAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RevokeToken));
            }

            if (options.EnableSessionEndpoints != false)
            {
                var session = group.MapGroup("/session");

                session.MapGet("/current", async ([FromServices] ISessionManagementHandler h, HttpContext ctx)
                    => await h.GetCurrentSessionAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.QuerySession));

                session.MapGet("/list", async ([FromServices] ISessionManagementHandler h, HttpContext ctx)
                    => await h.GetAllSessionsAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.QuerySession));

                session.MapPost("/revoke/{sessionId}", async ([FromServices] ISessionManagementHandler h, string sessionId, HttpContext ctx)
                    => await h.RevokeSessionAsync(sessionId, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RevokeSession));

                session.MapPost("/revoke-all", async ([FromServices] ISessionManagementHandler h, HttpContext ctx)
                    => await h.RevokeAllAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RevokeSession));
            }

            if (options.EnableUserInfoEndpoints != false)
            {
                var user = group.MapGroup("");

                user.MapGet("/userinfo", async ([FromServices] IUserInfoEndpointHandler h, HttpContext ctx)
                    => await h.GetUserInfoAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserInfo));

                user.MapGet("/permissions", async ([FromServices] IUserInfoEndpointHandler h, HttpContext ctx)
                    => await h.GetPermissionsAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.PermissionQuery));

                user.MapPost("/permissions/check", async ([FromServices] IUserInfoEndpointHandler h, HttpContext ctx)
                    => await h.CheckPermissionAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.PermissionQuery));
            }
        }

    }
}
