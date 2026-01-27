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
            // Default base: /auth
            string basePrefix = options.RoutePrefix.TrimStart('/');
            bool useRouteTenant = options.MultiTenant.Enabled && options.MultiTenant.EnableRoute;

            RouteGroupBuilder group = useRouteTenant
                ? rootGroup.MapGroup("/{tenant}/" + basePrefix)
                : rootGroup.MapGroup("/" + basePrefix);

            group.AddEndpointFilter<AuthFlowEndpointFilter>();

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

            if (options.EnablePkceEndpoints != false)
            {
                var pkce = group.MapGroup("/pkce");

                pkce.MapPost("/authorize", async ([FromServices] IPkceEndpointHandler h, HttpContext ctx)
                        => await h.AuthorizeAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Login));

                pkce.MapPost("/complete", async ([FromServices] IPkceEndpointHandler h, HttpContext ctx)
                        => await h.CompleteAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Login));
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

                session.MapPost("/current", async ([FromServices] ISessionManagementHandler h, HttpContext ctx)
                    => await h.GetCurrentSessionAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.QuerySession));

                session.MapPost("/list", async ([FromServices] ISessionManagementHandler h, HttpContext ctx)
                    => await h.GetAllSessionsAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.QuerySession));

                session.MapPost("/revoke/{sessionId}", async ([FromServices] ISessionManagementHandler h, string sessionId, HttpContext ctx)
                    => await h.RevokeSessionAsync(sessionId, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RevokeSession));

                session.MapPost("/revoke-all", async ([FromServices] ISessionManagementHandler h, HttpContext ctx)
                    => await h.RevokeAllAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RevokeSession));
            }

            if (options.EnableUserInfoEndpoints != false)
            {
                var user = group.MapGroup("");

                user.MapPost("/userinfo", async ([FromServices] IUserInfoEndpointHandler h, HttpContext ctx)
                    => await h.GetUserInfoAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserInfo));

                user.MapPost("/permissions", async ([FromServices] IUserInfoEndpointHandler h, HttpContext ctx)
                    => await h.GetPermissionsAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.PermissionQuery));

                user.MapPost("/permissions/check", async ([FromServices] IUserInfoEndpointHandler h, HttpContext ctx)
                    => await h.CheckPermissionAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.PermissionQuery));
            }

            if (options.EnableUserLifecycleEndpoints)
            {
                var users = group.MapGroup("/users");

                users.MapPost("/create", async ([FromServices] IUserLifecycleEndpointHandler h, HttpContext ctx)
                    => await h.CreateAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));

                users.MapPost("/status", async ([FromServices] IUserLifecycleEndpointHandler h, HttpContext ctx)
                    => await h.ChangeStatusAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));

                // Post is intended for Auth
                users.MapPost("/delete", async ([FromServices] IUserLifecycleEndpointHandler h, HttpContext ctx)
                    => await h.DeleteAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));
            }

            if (options.EnableUserProfileEndpoints)
            {
                var userProfile = group.MapGroup("/users");

                userProfile.MapPost("/me/get", async ([FromServices] IUserProfileEndpointHandler h, HttpContext ctx)
                    => await h.GetAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserProfile));

                userProfile.MapPost("/me/update", async ([FromServices] IUserProfileEndpointHandler h, HttpContext ctx)
                    => await h.UpdateAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserInfo));
            }

            if (options.EnableAdminChangeUserProfileEndpoints)
            {
                var admin = group.MapGroup("/admin/users");

                admin.MapPost("/{userKey}/profile/get", async ([FromServices] IUserProfileAdminEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.GetAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));

                admin.MapPost("/{userKey}/profile/update", async ([FromServices] IUserProfileAdminEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.UpdateAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));
            }

            if (options.EnableCredentialsEndpoints)
            {
                var credentials = group.MapGroup("/credentials");

                credentials.MapPost("/get", async ([FromServices] ICredentialEndpointHandler h, HttpContext ctx)
                    => await h.GetAllAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

                credentials.MapPost("/post", async ([FromServices] ICredentialEndpointHandler h, HttpContext ctx)
                    => await h.AddAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

                credentials.MapPost("/update/{type}", async ([FromServices] ICredentialEndpointHandler h, string type, HttpContext ctx)
                    => await h.ChangeAsync(type, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

                credentials.MapPost("/{type}/revoke", async ([FromServices] ICredentialEndpointHandler h, string type, HttpContext ctx)
                    => await h.RevokeAsync(type, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

                credentials.MapPost("/{type}/activate", async ([FromServices] ICredentialEndpointHandler h, string type, HttpContext ctx)
                    => await h.ActivateAsync(type, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

                credentials.MapPost("/delete/{type}", async ([FromServices] ICredentialEndpointHandler h, string type, HttpContext ctx)
                    => await h.DeleteAsync(type, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));
            }

            if (options.EnableAuthorizationEndpoints)
            {
                var authz = group.MapGroup("/authorization");

                authz.MapPost("/check", async ([FromServices] IAuthorizationEndpointHandler h, HttpContext ctx)
                    => await h.CheckAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.AuthorizationManagement));

                authz.MapPost("/users/{userKey}/roles/get", async ([FromServices] IAuthorizationEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.GetRolesAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.AuthorizationManagement));

                authz.MapPost("/users/{userKey}/roles/post", async ([FromServices] IAuthorizationEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.AssignRoleAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.AuthorizationManagement));

                authz.MapPost("/users/{userKey}/roles/delete", async ([FromServices] IAuthorizationEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.RemoveRoleAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.AuthorizationManagement));
            }

        }

    }
}
