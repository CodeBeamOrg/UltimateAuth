using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Defaults;

public static class UAuthActions
{
    public static string Create(string resource, string operation, ActionScope scope, string? subResource = null)
    {
        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("resource required");

        if (string.IsNullOrWhiteSpace(operation))
            throw new ArgumentException("operation required");

        var scopePart = scope.ToString().ToLowerInvariant();

        return subResource is null
            ? $"{resource}.{operation}.{scopePart}"
            : $"{resource}.{subResource}.{operation}.{scopePart}";
    }

    public static class Flows
    {
        public const string Wildcard = "flows.*";

        public const string LogoutSelf = "flows.logout.self";
        public const string LogoutDeviceSelf = "flows.logoutdevice.self";
        public const string LogoutDeviceAdmin = "flows.logoutdevice.admin";
        public const string LogoutOthersSelf = "flows.logoutothers.self";
        public const string LogoutOthersAdmin = "flows.logoutothers.admin";
        public const string LogoutAllSelf = "flows.logoutall.self";
        public const string LogoutAllAdmin = "flows.logoutall.admin";
    }

    public static class Sessions
    {
        public const string Wildcard = "sessions.*";

        public const string GetChainSelf = "sessions.getchain.self";
        public const string GetChainAdmin = "sessions.getchain.admin";
        public const string ListChainsSelf = "sessions.listchains.self";
        public const string ListChainsAdmin = "sessions.listchains.admin";
        public const string RevokeChainSelf = "sessions.revokechain.self";
        public const string RevokeChainAdmin = "sessions.revokechain.admin";
        public const string RevokeAllChainsSelf = "sessions.revokeallchains.self";
        public const string RevokeAllChainsAdmin = "sessions.revokeallchains.admin";
        public const string RevokeOtherChainsSelf = "sessions.revokeotherchains.self";
        public const string RevokeSessionAdmin = "sessions.revoke.admin";
        public const string RevokeRootAdmin = "sessions.revokeroot.admin";
    }

    public static class Users
    {
        public const string Wildcard = "users.*";

        public const string QueryAdmin = "users.query.admin";
        public const string CreateAnonymous = "users.create.anonymous";
        public const string CreateAdmin = "users.create.admin";
        public const string DeleteSelf = "users.delete.self";
        public const string DeleteAdmin = "users.delete.admin";
        public const string ChangeStatusSelf = "users.status.change.self";
        public const string ChangeStatusAdmin = "users.status.change.admin";
    }

    public static class UserProfiles
    {
        public const string Wildcard = "users.profile.*";

        public const string GetSelf = "users.profile.get.self";
        public const string UpdateSelf = "users.profile.update.self";
        public const string GetAdmin = "users.profile.get.admin";
        public const string UpdateAdmin = "users.profile.update.admin";
    }

    public static class UserIdentifiers
    {
        public const string Wildcard = "users.identifiers.*";

        public const string GetSelf = "users.identifiers.get.self";
        public const string GetAdmin = "users.identifiers.get.admin";
        public const string AddSelf = "users.identifiers.add.self";
        public const string AddAdmin = "users.identifiers.add.admin";
        public const string UpdateSelf = "users.identifiers.update.self";
        public const string UpdateAdmin = "users.identifiers.update.admin";
        public const string SetPrimarySelf = "users.identifiers.setprimary.self";
        public const string SetPrimaryAdmin = "users.identifiers.setprimary.admin";
        public const string UnsetPrimarySelf = "users.identifiers.unsetprimary.self";
        public const string UnsetPrimaryAdmin = "users.identifiers.unsetprimary.admin";
        public const string VerifySelf = "users.identifiers.verify.self";
        public const string VerifyAdmin = "users.identifiers.verify.admin";
        public const string DeleteSelf = "users.identifiers.delete.self";
        public const string DeleteAdmin = "users.identifiers.delete.admin";
    }

    public static class Credentials
    {
        public const string Wildcard = "credentials.*";

        public const string ListSelf = "credentials.list.self";
        public const string ListAdmin = "credentials.list.admin";
        public const string AddSelf = "credentials.add.self";
        public const string AddAdmin = "credentials.add.admin";
        public const string ChangeSelf = "credentials.change.self";
        public const string ChangeAdmin = "credentials.change.admin";
        public const string RevokeSelf = "credentials.revoke.self";
        public const string RevokeAdmin = "credentials.revoke.admin";
        public const string ActivateSelf = "credentials.activate.self";
        public const string BeginResetAnonymous = "credentials.beginreset.anonymous";
        public const string BeginResetAdmin = "credentials.beginreset.admin";
        public const string CompleteResetAnonymous = "credentials.completereset.anonymous";
        public const string CompleteResetAdmin = "credentials.completereset.admin";
        public const string DeleteAdmin = "credentials.delete.admin";
    }

    public static class Authorization
    {
        public const string Wildcard = "authorization.*";

        public static class Roles
        {
            public const string GetSelf = "authorization.roles.get.self";
            public const string GetAdmin = "authorization.roles.get.admin";
            public const string AssignAdmin = "authorization.roles.assign.admin";
            public const string RemoveAdmin = "authorization.roles.remove.admin";
            public const string CreateAdmin = "authorization.roles.create.admin";
            public const string RenameAdmin = "authorization.roles.rename.admin";
            public const string DeleteAdmin = "authorization.roles.delete.admin";
            public const string SetPermissionsAdmin = "authorization.roles.permissions.admin";
            public const string QueryAdmin = "authorization.roles.query.admin";
        }
    }
}
