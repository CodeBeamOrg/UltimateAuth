namespace CodeBeam.UltimateAuth.Server.Defaults
{
    public static class UAuthActions
    {
        public static class Users
        {
            public const string Create = "users.create";
            public const string Delete = "users.delete";
            public const string ChangeStatus = "users.status.change";
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
            public const string Change = "users.identifiers.change";
            public const string Verify = "users.identifiers.verify";
            public const string Delete = "users.identifiers.delete";
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

    }
}
