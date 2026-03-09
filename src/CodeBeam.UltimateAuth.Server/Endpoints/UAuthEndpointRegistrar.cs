using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

// TODO: Add Scalar/Swagger integration
// TODO: Add endpoint based guards
public class UAuthEndpointRegistrar : IAuthEndpointRegistrar
{

    // NOTE:
    // All endpoints intentionally use POST to avoid caching,
    // CSRF ambiguity, and credential leakage via query strings.
    public void MapEndpoints(RouteGroupBuilder rootGroup, UAuthServerOptions options)
    {
        // Default base: /auth
        string basePrefix = options.Endpoints.BasePath.TrimStart('/');
        bool useRouteTenant = options.MultiTenant.Enabled && options.MultiTenant.EnableRoute;

        RouteGroupBuilder group = useRouteTenant
            ? rootGroup.MapGroup("/{tenant}/" + basePrefix)
            : rootGroup.MapGroup("/" + basePrefix);

        group.AddEndpointFilter<AuthFlowEndpointFilter>();

        //var user = group.MapGroup("");
        var users = group.MapGroup("/users");
        var adminUsers = group.MapGroup("/admin/users");

        if (options.Endpoints.Login != false)
        {
            group.MapPost("/login", async ([FromServices] ILoginEndpointHandler h, HttpContext ctx)
                => await h.LoginAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Login));

            group.MapPost("/validate", async ([FromServices] IValidateEndpointHandler h, HttpContext ctx)
                => await h.ValidateAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.ValidateSession));

            group.MapPost("/logout", async ([FromServices] ILogoutEndpointHandler h, HttpContext ctx)
                => await h.LogoutAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Logout));

            group.MapPost("/logout-device", async ([FromServices] ILogoutEndpointHandler h, HttpContext ctx)
                => await h.LogoutDeviceSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Logout));

            group.MapPost("/logout-others", async ([FromServices] ILogoutEndpointHandler h, HttpContext ctx)
                => await h.LogoutOthersSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Logout));

            group.MapPost("/logout-all", async ([FromServices] ILogoutEndpointHandler h, HttpContext ctx)
                => await h.LogoutAllSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Logout));

            group.MapPost("/refresh", async ([FromServices] IRefreshEndpointHandler h, HttpContext ctx)
                => await h.RefreshAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RefreshSession));

            group.MapPost("/reauth", async ([FromServices] IReauthEndpointHandler h, HttpContext ctx)
                => await h.ReauthAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Reauthentication));


            adminUsers.MapPost("/logout-device/{userKey}", async ([FromServices] ILogoutEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.LogoutDeviceAdminAsync(ctx, userKey)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Logout));

            adminUsers.MapPost("/logout-others/{userKey}", async ([FromServices] ILogoutEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.LogoutOthersAdminAsync(ctx, userKey)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Logout));

            adminUsers.MapPost("/logout-all/{userKey}", async ([FromServices] ILogoutEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.LogoutAllAdminAsync(ctx, userKey)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Logout));
        }

        if (options.Endpoints.Pkce != false)
        {
            var pkce = group.MapGroup("/pkce");

            pkce.MapPost("/authorize", async ([FromServices] IPkceEndpointHandler h, HttpContext ctx)
                    => await h.AuthorizeAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Login));

            pkce.MapPost("/complete", async ([FromServices] IPkceEndpointHandler h, HttpContext ctx)
                    => await h.CompleteAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Login));
        }

        if (options.Endpoints.Token != false)
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

        if (options.Endpoints.Session != false)
        {
            var session = group.MapGroup("/session");

            session.MapPost("/me/chains", async ([FromServices] ISessionEndpointHandler h, HttpContext ctx)
                => await h.GetMyChainsAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.QuerySession));

            session.MapPost("/me/chains/{chainId}", async ([FromServices] ISessionEndpointHandler h, SessionChainId chainId, HttpContext ctx)
                => await h.GetMyChainDetailAsync(chainId, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.QuerySession));

            session.MapPost("/me/chains/{chainId}/revoke", async ([FromServices] ISessionEndpointHandler h, SessionChainId chainId, HttpContext ctx)
                => await h.RevokeMyChainAsync(chainId, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RevokeSession));

            session.MapPost("/me/revoke-others",async ([FromServices] ISessionEndpointHandler h, HttpContext ctx)
                => await h.RevokeOtherChainsAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RevokeSession));

            session.MapPost("/me/revoke-all", async ([FromServices] ISessionEndpointHandler h, HttpContext ctx)
                => await h.RevokeAllMyChainsAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RevokeSession));


            adminUsers.MapPost("/{userKey}/sessions/chains", async ([FromServices] ISessionEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.GetUserChainsAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.QuerySession));

            adminUsers.MapPost("/{userKey}/sessions/chains/{chainId}", async ([FromServices] ISessionEndpointHandler h, UserKey userKey, SessionChainId chainId, HttpContext ctx)
                => await h.GetUserChainDetailAsync(userKey, chainId, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.QuerySession));

            adminUsers.MapPost("/{userKey}/sessions/{sessionId}/revoke", async ([FromServices] ISessionEndpointHandler h, UserKey userKey, AuthSessionId sessionId, HttpContext ctx)
                => await h.RevokeUserSessionAsync(userKey, sessionId, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RevokeSession));

            adminUsers.MapPost("/{userKey}/sessions/chains/{chainId}/revoke", async ([FromServices] ISessionEndpointHandler h, UserKey userKey, SessionChainId chainId, HttpContext ctx)
                => await h.RevokeUserChainAsync(userKey, chainId, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RevokeSession));

            adminUsers.MapPost("/{userKey}/sessions/revoke-root", async ([FromServices] ISessionEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.RevokeRootAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RevokeSession));

            adminUsers.MapPost("/{userKey}/sessions/revoke-all", async ([FromServices] ISessionEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.RevokeAllChainsAsync(userKey, null, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RevokeSession));
        }

        //if (options.EnableUserInfoEndpoints != false)
        //{
        //    user.MapPost("/userinfo", async ([FromServices] IUserInfoEndpointHandler h, HttpContext ctx)
        //        => await h.GetUserInfoAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserInfo));

        //    user.MapPost("/permissions", async ([FromServices] IUserInfoEndpointHandler h, HttpContext ctx)
        //        => await h.GetPermissionsAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.PermissionQuery));

        //    user.MapPost("/permissions/check", async ([FromServices] IUserInfoEndpointHandler h, HttpContext ctx)
        //        => await h.CheckPermissionAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.PermissionQuery));
        //}

        if (options.Endpoints.UserLifecycle != false)
        {
            users.MapPost("/create", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                => await h.CreateAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));

            users.MapPost("/me/status", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                => await h.ChangeStatusSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));

            users.MapPost("/me/delete", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                => await h.DeleteMeAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));


            adminUsers.MapPost("/{userKey}/status", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.ChangeStatusAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));

            // Post is intended for Auth
            adminUsers.MapPost("/{userKey}/delete", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.DeleteAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));
        }

        if (options.Endpoints.UserProfile != false)
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

        if (options.Endpoints.UserIdentifier != false)
        {
            users.MapPost("/me/identifiers/get", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                => await h.GetMyIdentifiersAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

            users.MapPost("/me/identifiers/exists", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                => await h.IdentifierExistsSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

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

            adminUsers.MapPost("/{userKey}/identifiers/exists", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.IdentifierExistsAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

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

        if (options.Endpoints.Credentials != false)
        {
            var credentials = group.MapGroup("/credentials");
            var adminCredentials = group.MapGroup("/admin/users/{userKey}/credentials");

            credentials.MapPost("/get", async ([FromServices] ICredentialEndpointHandler h, HttpContext ctx)
                => await h.GetAllAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

            credentials.MapPost("/add", async ([FromServices] ICredentialEndpointHandler h, HttpContext ctx)
                => await h.AddAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

            credentials.MapPost("/change", async ([FromServices] ICredentialEndpointHandler h, HttpContext ctx)
                => await h.ChangeSecretAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

            credentials.MapPost("/revoke", async ([FromServices] ICredentialEndpointHandler h, HttpContext ctx)
                => await h.RevokeAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

            credentials.MapPost("/reset/begin", async ([FromServices] ICredentialEndpointHandler h, HttpContext ctx)
                => await h.BeginResetAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

            credentials.MapPost("/reset/complete", async ([FromServices] ICredentialEndpointHandler h, HttpContext ctx)
                => await h.CompleteResetAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));


            adminCredentials.MapPost("/get", async ([FromServices] ICredentialEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.GetAllAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

            adminCredentials.MapPost("/add", async ([FromServices] ICredentialEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.AddAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

            adminCredentials.MapPost("/revoke", async ([FromServices] ICredentialEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.RevokeAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

            adminCredentials.MapPost("/reset/begin", async ([FromServices] ICredentialEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.BeginResetAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

            adminCredentials.MapPost("/reset/complete", async ([FromServices] ICredentialEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.CompleteResetAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

            adminCredentials.MapPost("/delete", async ([FromServices] ICredentialEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.DeleteAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));
        }

        if (options.Endpoints.Authorization != false)
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

        // IMPORTANT:
        // Escape hatch is invoked AFTER all UltimateAuth endpoints are registered.
        // Developers may add metadata, filters, authorization, rate limits, etc.
        // Removing or remapping UltimateAuth endpoints is unsupported.
        options.OnConfigureEndpoints?.Invoke(rootGroup);
    }
}
