using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Authorization.Domain;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

// TODO: Add Scalar/Swagger integration
// TODO: Add endpoint based guards
public class UAuthEndpointRegistrar : IAuthEndpointRegistrar
{
    private readonly UAuthServerEndpointOptions _options;
    public UAuthEndpointRegistrar(IOptions<UAuthServerOptions> options)
    {
        _options = options.Value.Endpoints;
    }

    bool Enabled(string action) => !_options.DisabledActions.Contains(action);

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
        var self = group.MapGroup("/me");
        var admin = group.MapGroup("/admin");

        if (options.Endpoints.Authentication != false)
        {
            group.MapPost("/login", async ([FromServices] ILoginEndpointHandler h, HttpContext ctx)
                => await h.LoginAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Login));

            group.MapPost("/validate", async ([FromServices] IValidateEndpointHandler h, HttpContext ctx)
                => await h.ValidateAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.ValidateSession));

            if (Enabled(UAuthActions.Flows.LogoutSelf))
                group.MapPost("/logout", async ([FromServices] ILogoutEndpointHandler h, HttpContext ctx)
                => await h.LogoutAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Logout));

            if (Enabled(UAuthActions.Flows.LogoutDeviceSelf))
                self.MapPost("/logout-device", async ([FromServices] ILogoutEndpointHandler h, HttpContext ctx)
                => await h.LogoutDeviceSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Logout));

            if (Enabled(UAuthActions.Flows.LogoutOthersSelf))
                self.MapPost("/logout-others", async ([FromServices] ILogoutEndpointHandler h, HttpContext ctx)
                => await h.LogoutOthersSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Logout));

            if (Enabled(UAuthActions.Flows.LogoutAllSelf))
                self.MapPost("/logout-all", async ([FromServices] ILogoutEndpointHandler h, HttpContext ctx)
                => await h.LogoutAllSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Logout));

            group.MapPost("/refresh", async ([FromServices] IRefreshEndpointHandler h, HttpContext ctx)
                => await h.RefreshAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RefreshSession));

            group.MapPost("/reauth", async ([FromServices] IReauthEndpointHandler h, HttpContext ctx)
                => await h.ReauthAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Reauthentication));


            if (Enabled(UAuthActions.Flows.LogoutDeviceAdmin))
                admin.MapPost("/users/{userKey}/logout-device", async ([FromServices] ILogoutEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.LogoutDeviceAdminAsync(ctx, userKey)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Logout));

            if (Enabled(UAuthActions.Flows.LogoutOthersAdmin))
                admin.MapPost("/users/{userKey}/logout-others", async ([FromServices] ILogoutEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.LogoutOthersAdminAsync(ctx, userKey)).WithMetadata(new AuthFlowMetadata(AuthFlowType.Logout));

            if (Enabled(UAuthActions.Flows.LogoutAllAdmin))
                admin.MapPost("/users/{userKey}/logout-all", async ([FromServices] ILogoutEndpointHandler h, UserKey userKey, HttpContext ctx)
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

        //if (options.Endpoints.Token != false)
        //{
        //    var token = group.MapGroup("");

        //    token.MapPost("/token", async ([FromServices] ITokenEndpointHandler h, HttpContext ctx)
        //        => await h.GetTokenAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.IssueToken));

        //    token.MapPost("/refresh-token", async ([FromServices] ITokenEndpointHandler h, HttpContext ctx)
        //        => await h.RefreshTokenAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RefreshToken));

        //    token.MapPost("/introspect", async ([FromServices] ITokenEndpointHandler h, HttpContext ctx)
        //        => await h.IntrospectAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.IntrospectToken));

        //    token.MapPost("/revoke", async ([FromServices] ITokenEndpointHandler h, HttpContext ctx)
        //        => await h.RevokeAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RevokeToken));
        //}

        if (options.Endpoints.Session != false)
        {
            var selfSession = self.MapGroup("/sessions");
            var adminSession = admin.MapGroup("/users/{userKey}/sessions");

            if (Enabled(UAuthActions.Sessions.ListChainsSelf))
                selfSession.MapPost("/chains", async ([FromServices] ISessionEndpointHandler h, HttpContext ctx)
                => await h.GetMyChainsAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.QuerySession));

            if (Enabled(UAuthActions.Sessions.GetChainSelf))
                selfSession.MapPost("/chains/{chainId}", async ([FromServices] ISessionEndpointHandler h, SessionChainId chainId, HttpContext ctx)
                => await h.GetMyChainDetailAsync(chainId, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.QuerySession));

            if (Enabled(UAuthActions.Sessions.RevokeChainSelf))
                selfSession.MapPost("/chains/{chainId}/revoke", async ([FromServices] ISessionEndpointHandler h, SessionChainId chainId, HttpContext ctx)
                => await h.RevokeMyChainAsync(chainId, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RevokeSession));

            if (Enabled(UAuthActions.Sessions.RevokeOtherChainsSelf))
                selfSession.MapPost("/revoke-others",async ([FromServices] ISessionEndpointHandler h, HttpContext ctx)
                => await h.RevokeOtherChainsAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RevokeSession));

            if (Enabled(UAuthActions.Sessions.RevokeAllChainsSelf))
                selfSession.MapPost("/revoke-all", async ([FromServices] ISessionEndpointHandler h, HttpContext ctx)
                => await h.RevokeAllMyChainsAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RevokeSession));


            if (Enabled(UAuthActions.Sessions.ListChainsAdmin))
                adminSession.MapPost("/chains", async ([FromServices] ISessionEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.GetUserChainsAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.QuerySession));

            if (Enabled(UAuthActions.Sessions.GetChainAdmin))
                adminSession.MapPost("/chains/{chainId}", async ([FromServices] ISessionEndpointHandler h, UserKey userKey, SessionChainId chainId, HttpContext ctx)
                => await h.GetUserChainDetailAsync(userKey, chainId, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.QuerySession));

            if (Enabled(UAuthActions.Sessions.RevokeSessionAdmin))
                adminSession.MapPost("/{sessionId}/revoke", async ([FromServices] ISessionEndpointHandler h, UserKey userKey, AuthSessionId sessionId, HttpContext ctx)
                => await h.RevokeUserSessionAsync(userKey, sessionId, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RevokeSession));

            if (Enabled(UAuthActions.Sessions.RevokeChainAdmin))
                adminSession.MapPost("/chains/{chainId}/revoke", async ([FromServices] ISessionEndpointHandler h, UserKey userKey, SessionChainId chainId, HttpContext ctx)
                => await h.RevokeUserChainAsync(userKey, chainId, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RevokeSession));

            if (Enabled(UAuthActions.Sessions.RevokeRootAdmin))
                adminSession.MapPost("/revoke-root", async ([FromServices] ISessionEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.RevokeRootAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.RevokeSession));

            if (Enabled(UAuthActions.Sessions.RevokeAllChainsAdmin))
                adminSession.MapPost("/revoke-all", async ([FromServices] ISessionEndpointHandler h, UserKey userKey, HttpContext ctx)
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

        var adminUsers = admin.MapGroup("/users");

        if (options.Endpoints.UserLifecycle != false)
        {
            if (Enabled(UAuthActions.Users.CreateAnonymous))
                group.MapPost("/users/create", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                => await h.CreateAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));

            if (Enabled(UAuthActions.Users.ChangeStatusSelf))
                self.MapPost("/status", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                => await h.ChangeStatusSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));

            if (Enabled(UAuthActions.Users.DeleteSelf))
                self.MapPost("/delete", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                => await h.DeleteMeAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));


            if (Enabled(UAuthActions.Users.QueryAdmin))
                adminUsers.MapPost("/query", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                => await h.QueryUsersAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));

            if (Enabled(UAuthActions.Users.CreateAdmin))
                adminUsers.MapPost("/create", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                => await h.CreateAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));

            if (Enabled(UAuthActions.Users.ChangeStatusAdmin))
                adminUsers.MapPost("/{userKey}/status", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.ChangeStatusAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));

            if (Enabled(UAuthActions.Users.DeleteAdmin))
                adminUsers.MapPost("/{userKey}/delete", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.DeleteAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserManagement));
        }

        if (options.Endpoints.UserProfile != false)
        {
            if (Enabled(UAuthActions.UserProfiles.GetSelf))
                self.MapPost("/get", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                => await h.GetMeAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserProfileManagement));

            if (Enabled(UAuthActions.UserProfiles.UpdateSelf))
                self.MapPost("/update", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                => await h.UpdateMeAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserProfileManagement));


            if (Enabled(UAuthActions.UserProfiles.GetAdmin))
                adminUsers.MapPost("/{userKey}/profile/get", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.GetUserAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserProfileManagement));

            if (Enabled(UAuthActions.UserProfiles.UpdateAdmin))
                adminUsers.MapPost("/{userKey}/profile/update", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.UpdateUserAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserProfileManagement));
        }

        if (options.Endpoints.UserIdentifier != false)
        {
            if (Enabled(UAuthActions.UserIdentifiers.GetSelf))
                self.MapPost("/identifiers/get", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                => await h.GetMyIdentifiersAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

            if (Enabled(UAuthActions.UserIdentifiers.AddSelf))
                self.MapPost("/identifiers/add", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                => await h.AddUserIdentifierSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

            if (Enabled(UAuthActions.UserIdentifiers.UpdateSelf))
                self.MapPost("/identifiers/update", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                => await h.UpdateUserIdentifierSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

            if (Enabled(UAuthActions.UserIdentifiers.SetPrimarySelf))
                self.MapPost("/identifiers/set-primary",async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                => await h.SetPrimaryUserIdentifierSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

            if (Enabled(UAuthActions.UserIdentifiers.UnsetPrimarySelf))
                self.MapPost("/identifiers/unset-primary", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                => await h.UnsetPrimaryUserIdentifierSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

            if (Enabled(UAuthActions.UserIdentifiers.VerifySelf))
                self.MapPost("/identifiers/verify", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                => await h.VerifyUserIdentifierSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

            if (Enabled(UAuthActions.UserIdentifiers.DeleteSelf))
                self.MapPost("/identifiers/delete", async ([FromServices] IUserEndpointHandler h, HttpContext ctx)
                => await h.DeleteUserIdentifierSelfAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));


            if (Enabled(UAuthActions.UserIdentifiers.GetAdmin))
                adminUsers.MapPost("/{userKey}/identifiers/get", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.GetUserIdentifiersAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

            if (Enabled(UAuthActions.UserIdentifiers.AddAdmin))
                adminUsers.MapPost("/{userKey}/identifiers/add", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.AddUserIdentifierAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

            if (Enabled(UAuthActions.UserIdentifiers.UpdateAdmin))
                adminUsers.MapPost("/{userKey}/identifiers/update", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.UpdateUserIdentifierAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

            if (Enabled(UAuthActions.UserIdentifiers.SetPrimaryAdmin))
                adminUsers.MapPost("/{userKey}/identifiers/set-primary", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.SetPrimaryUserIdentifierAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

            if (Enabled(UAuthActions.UserIdentifiers.UnsetPrimaryAdmin))
                adminUsers.MapPost("/{userKey}/identifiers/unset-primary", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.UnsetPrimaryUserIdentifierAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

            if (Enabled(UAuthActions.UserIdentifiers.VerifyAdmin))
                adminUsers.MapPost("/{userKey}/identifiers/verify", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.VerifyUserIdentifierAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));

            if (Enabled(UAuthActions.UserIdentifiers.DeleteAdmin))
                adminUsers.MapPost("/{userKey}/identifiers/delete", async ([FromServices] IUserEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.DeleteUserIdentifierAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.UserIdentifierManagement));
        }

        if (options.Endpoints.Credentials != false)
        {
            var selfCredentials = self.MapGroup("/credentials");
            var adminCredentials = admin.MapGroup("/users/{userKey}/credentials");

            if (Enabled(UAuthActions.Credentials.AddSelf))
                selfCredentials.MapPost("/add", async ([FromServices] ICredentialEndpointHandler h, HttpContext ctx)
                => await h.AddAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

            if (Enabled(UAuthActions.Credentials.ChangeSelf))
                selfCredentials.MapPost("/change", async ([FromServices] ICredentialEndpointHandler h, HttpContext ctx)
                => await h.ChangeSecretAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

            if (Enabled(UAuthActions.Credentials.RevokeSelf))
                selfCredentials.MapPost("/revoke", async ([FromServices] ICredentialEndpointHandler h, HttpContext ctx)
                => await h.RevokeAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

            if (Enabled(UAuthActions.Credentials.BeginResetAnonymous))
                selfCredentials.MapPost("/reset/begin", async ([FromServices] ICredentialEndpointHandler h, HttpContext ctx)
                => await h.BeginResetAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

            if (Enabled(UAuthActions.Credentials.CompleteResetAnonymous))
                selfCredentials.MapPost("/reset/complete", async ([FromServices] ICredentialEndpointHandler h, HttpContext ctx)
                => await h.CompleteResetAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));


            if (Enabled(UAuthActions.Credentials.AddAdmin))
                adminCredentials.MapPost("/add", async ([FromServices] ICredentialEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.AddAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

            if (Enabled(UAuthActions.Credentials.ChangeAdmin))
                adminCredentials.MapPost("/change", async ([FromServices] ICredentialEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.ChangeSecretAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

            if (Enabled(UAuthActions.Credentials.RevokeAdmin))
                adminCredentials.MapPost("/revoke", async ([FromServices] ICredentialEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.RevokeAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

            if (Enabled(UAuthActions.Credentials.BeginResetAdmin))
                adminCredentials.MapPost("/reset/begin", async ([FromServices] ICredentialEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.BeginResetAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

            if (Enabled(UAuthActions.Credentials.CompleteResetAdmin))
                adminCredentials.MapPost("/reset/complete", async ([FromServices] ICredentialEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.CompleteResetAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));

            if (Enabled(UAuthActions.Credentials.DeleteAdmin))
                adminCredentials.MapPost("/delete", async ([FromServices] ICredentialEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.DeleteAdminAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.CredentialManagement));
        }

        if (options.Endpoints.Authorization != false)
        {
            var selfAuthz = self.MapGroup("/authorization");
            var adminAuthz = admin.MapGroup("/authorization");

            // TODO: Add enabled actions here
                selfAuthz.MapPost("/check", async ([FromServices] IAuthorizationEndpointHandler h, HttpContext ctx)
                => await h.CheckAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.AuthorizationManagement));

            if (Enabled(UAuthActions.Authorization.Roles.ReadSelf))
                selfAuthz.MapPost("/roles/get", async ([FromServices] IAuthorizationEndpointHandler h, HttpContext ctx)
                => await h.GetMyRolesAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.AuthorizationManagement));


            if (Enabled(UAuthActions.Authorization.Roles.ReadAdmin))
                adminAuthz.MapPost("/users/{userKey}/roles/get", async ([FromServices] IAuthorizationEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.GetUserRolesAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.AuthorizationManagement));

            if (Enabled(UAuthActions.Authorization.Roles.AssignAdmin))
                adminAuthz.MapPost("/users/{userKey}/roles/assign", async ([FromServices] IAuthorizationEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.AssignRoleAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.AuthorizationManagement));

            if (Enabled(UAuthActions.Authorization.Roles.RemoveAdmin))
                adminAuthz.MapPost("/users/{userKey}/roles/remove", async ([FromServices] IAuthorizationEndpointHandler h, UserKey userKey, HttpContext ctx)
                => await h.RemoveRoleAsync(userKey, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.AuthorizationManagement));

            if (Enabled(UAuthActions.Authorization.Roles.CreateAdmin))
                adminAuthz.MapPost("/roles/create",async ([FromServices] IAuthorizationEndpointHandler h, HttpContext ctx)
                    => await h.CreateRoleAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.AuthorizationManagement));

            if (Enabled(UAuthActions.Authorization.Roles.QueryAdmin))
                adminAuthz.MapPost("/roles/query", async ([FromServices] IAuthorizationEndpointHandler h, HttpContext ctx)
                    => await h.QueryRolesAsync(ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.AuthorizationManagement));

            if (Enabled(UAuthActions.Authorization.Roles.RenameAdmin))
                adminAuthz.MapPost("/roles/{roleId}/rename", async ([FromServices] IAuthorizationEndpointHandler h, RoleId roleId, HttpContext ctx)
                    => await h.RenameRoleAsync(roleId, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.AuthorizationManagement));

            if (Enabled(UAuthActions.Authorization.Roles.SetPermissionsAdmin))
                adminAuthz.MapPost("/roles/{roleId}/permissions", async ([FromServices] IAuthorizationEndpointHandler h, RoleId roleId, HttpContext ctx)
                    => await h.SetRolePermissionsAsync(roleId, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.AuthorizationManagement));

            if (Enabled(UAuthActions.Authorization.Roles.DeleteAdmin))
                adminAuthz.MapPost("/roles/{roleId}/delete", async ([FromServices] IAuthorizationEndpointHandler h, RoleId roleId, HttpContext ctx)
                    => await h.DeleteRoleAsync(roleId, ctx)).WithMetadata(new AuthFlowMetadata(AuthFlowType.AuthorizationManagement));
        }

        // IMPORTANT:
        // Escape hatch is invoked AFTER all UltimateAuth endpoints are registered.
        // Developers may add metadata, filters, authorization, rate limits, etc.
        // Removing or remapping UltimateAuth endpoints is unsupported.
        options.OnConfigureEndpoints?.Invoke(rootGroup);
    }
}
