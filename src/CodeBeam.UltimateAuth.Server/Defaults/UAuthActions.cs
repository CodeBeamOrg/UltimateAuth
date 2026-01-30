namespace CodeBeam.UltimateAuth.Server.Defaults
{
    public static class UAuthActions
    {
        public static class Users
        {
            public const string Create = "users.create";
            public const string DeleteAdmin = "users.delete.admin";
            public const string ChangeStatusSelf = "users.status.change.self";
            public const string ChangeStatusAdmin = "users.status.change.admin";
        }

        public static class UserProfiles
        {
            public const string GetSelf = "users.profile.get.self";
            public const string UpdateSelf = "users.profile.update.self";
            public const string GetAdmin = "users.profile.get.admin";
            public const string UpdateAdmin = "users.profile.update.admin";
        }

        public static class UserIdentifiers
        {
            public const string Get = "users.identifiers.get";
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
            public const string List = "credentials.list";
            public const string Add = "credentials.add";
            public const string Change = "credentials.change";
            public const string Revoke = "credentials.revoke";
            public const string Activate = "credentials.activate";
            public const string Delete = "credentials.delete";
        }

        public static class Authorization
        {
            public static class Roles
            {
                public const string Read = "authorization.roles.read";
                public const string Assign = "authorization.roles.assign";
                public const string Remove = "authorization.roles.remove";
            }
        }

    }
}
