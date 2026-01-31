using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using static CodeBeam.UltimateAuth.Server.Defaults.UAuthActions;

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

            var user = group.MapGroup("");
            var users = group.MapGroup("/users");
            var adminUsers = group.MapGroup("/admin/users");

            //if (options.EnableUserInfoEndpoints != false)
            //{
            //    user.MapPost("/userinfo", async ([FromServices] IUserInfoEndpointHandler h, HttpContext ctx)
            //        => await h.GetUserInfoAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserInfo));

            //    user.MapPost("/permissions", async ([FromServices] IUserInfoEndpointHandler h, HttpContext ctx)
            //        => await h.GetPermissionsAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.PermissionQuery));

            //    user.MapPost("/permissions/check", async ([FromServices] IUserInfoEndpointHandler h, HttpContext ctx)
            //        => await h.CheckPermissionAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.PermissionQuery));
            //}

            if (options.EnableUserLifecycleEndpoints != false)
            {
                users.MapPost("/create", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                    => await h.CreateAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));

                users.MapPost("/me/status", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                    => await h.ChangeStatusSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));

                adminUsers.MapPost("/{userKey}/status", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.ChangeStatusAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));

                // Post is intended for Auth
                adminUsers.MapPost("/{userKey}/delete", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.DeleteAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));
            }

            if (options.EnableUserProfileEndpoints != false)
            {
                users.MapPost("/me/get", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                    => await h.GetMeAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserProfileManagement));

                users.MapPost("/me/update", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                    => await h.UpdateMeAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserProfileManagement));

                adminUsers.MapPost("/{userKey}/profile/get", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.GetUserAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserProfileManagement));

                adminUsers.MapPost("/{userKey}/profile/update", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.UpdateUserAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserProfileManagement));
            }

            if (options.EnableUserIdentifierEndpoints != false)
            {
                users.MapPost("/me/identifiers/get", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                    => await h.GetMyIdentifiersAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

                users.MapPost("/me/identifiers/add", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                    => await h.AddUserIdentifierSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

                users.MapPost("/me/identifiers/update", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                    => await h.UpdateUserIdentifierSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

                users.MapPost("/me/identifiers/set-primary",async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                    => await h.SetPrimaryUserIdentifierSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

                users.MapPost("/me/identifiers/unset-primary", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                    => await h.UnsetPrimaryUserIdentifierSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

                users.MapPost("/me/identifiers/verify", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                    => await h.VerifyUserIdentifierSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

                users.MapPost("/me/identifiers/delete", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                    => await h.DeleteUserIdentifierSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));


                adminUsers.MapPost("/{userKey}/identifiers/get", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.GetUserIdentifiersAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

                adminUsers.MapPost("/{userKey}/identifiers/add", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.AddUserIdentifierAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

                adminUsers.MapPost("/{userKey}/identifiers/update", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.UpdateUserIdentifierAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

                adminUsers.MapPost("/{userKey}/identifiers/set-primary", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.SetPrimaryUserIdentifierAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

                adminUsers.MapPost("/{userKey}/identifiers/unset-primary", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.UnsetPrimaryUserIdentifierAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

                adminUsers.MapPost("/{userKey}/identifiers/verify", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.VerifyUserIdentifierAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

                adminUsers.MapPost("/{userKey}/identifiers/delete", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.DeleteUserIdentifierAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));
            }

            if (options.EnableCredentialsEndpoints != false)
            {
                var credentials = group.MapGroup("/credentials");
                var adminCredentials = group.MapGroup("/admin/users/{userKey}/credentials");

                credentials.MapPost("/get", async ([FromServices] ICredentialEndpointHandler h, HttpContext ctx)
                    => await h.GetAllAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

                credentials.MapPost("/add", async ([FromServices] ICredentialEndpointHandler h, HttpContext ctx)
                    => await h.AddAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

                credentials.MapPost("/{type}/change", async ([FromServices] ICredentialEndpointHandler h, string type, HttpContext ctx)
                    => await h.ChangeAsync(type, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

                credentials.MapPost("/{type}/revoke", async ([FromServices] ICredentialEndpointHandler h, string type, HttpContext ctx)
                    => await h.RevokeAsync(type, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

                credentials.MapPost("/{type}/reset/begin", async ([FromServices] ICredentialEndpointHandler h, string type, HttpContext ctx)
                    => await h.BeginResetAsync(type, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

                credentials.MapPost("/{type}/reset/complete", async ([FromServices] ICredentialEndpointHandler h, string type, HttpContext ctx)
                    => await h.CompleteResetAsync(type, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));


                adminCredentials.MapPost("/get", async ([FromServices] ICredentialEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.GetAllAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

                adminCredentials.MapPost("/add", async ([FromServices] ICredentialEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.AddAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

                adminCredentials.MapPost("/{type}/revoke", async ([FromServices] ICredentialEndpointHandler h, UserKey userKey, string type, HttpContext ctx)
                    => await h.RevokeAdminAsync(userKey, type, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

                adminCredentials.MapPost("/{type}/activate", async ([FromServices] ICredentialEndpointHandler h, UserKey userKey, string type, HttpContext ctx)
                    => await h.ActivateAdminAsync(userKey, type, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

                adminCredentials.MapPost("/{type}/reset/begin", async ([FromServices] ICredentialEndpointHandler h, UserKey userKey, string type, HttpContext ctx)
                    => await h.BeginResetAdminAsync(userKey, type, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

                adminCredentials.MapPost("/{type}/reset/complete", async ([FromServices] ICredentialEndpointHandler h, UserKey userKey, string type, HttpContext ctx)
                    => await h.CompleteResetAdminAsync(userKey, type, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

                adminCredentials.MapPost("/{type}/delete", async ([FromServices] ICredentialEndpointHandler h, UserKey userKey, string type, HttpContext ctx)
                    => await h.DeleteAdminAsync(userKey, type, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));
            }

            if (options.EnableAuthorizationEndpoints != false)
            {
                var authz = group.MapGroup("/authorization");
                var adminAuthz = group.MapGroup("/admin/authorization");

                authz.MapPost("/check", async ([FromServices] IAuthorizationEndpointHandler h, HttpContext ctx)
                    => await h.CheckAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.AuthorizationManagement));

                authz.MapPost("/users/me/roles/get", async ([FromServices] IAuthorizationEndpointHandler h, HttpContext ctx)
                    => await h.GetMyRolesAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.AuthorizationManagement));


                adminAuthz.MapPost("/users/{userKey}/roles/get", async ([FromServices] IAuthorizationEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.GetUserRolesAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.AuthorizationManagement));

                adminAuthz.MapPost("/users/{userKey}/roles/post", async ([FromServices] IAuthorizationEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.AssignRoleAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.AuthorizationManagement));

                adminAuthz.MapPost("/users/{userKey}/roles/delete", async ([FromServices] IAuthorizationEndpointHandler h, UserKey userKey, HttpContext ctx)
                    => await h.RemoveRoleAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.AuthorizationManagement));
            }

        }

    }
}
