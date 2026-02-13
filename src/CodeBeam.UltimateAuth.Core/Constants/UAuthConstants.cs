namespace CodeBeam.UltimateAuth.Core.Constants;

public static class UAuthConstants
{
    public static class HttpItems
    {
        public const string SessionContext = "__UAuth.SessionContext";
        public const string TenantContextKey = "__UAuthTenant";
        public const string UserContextKey = "__UAuthUser";
    }

    public static class Form
    {
        public const string ReturnUrl = "return_url";
        public const string Device = "__uauth_device";
        public const string ClientProfile = "__uauth_client_profile";
    }

    public static class Query
    {
        public const string ReturnUrl = "return_url";
        public const string Hub = "hub";
    }

    public static class Headers
    {
        public const string ClientProfile = "X-UAuth-ClientProfile";
        public const string DeviceId = "X-UDID";
        public const string Refresh = "X-UAuth-Refresh";
        public const string AuthState = "X-UAuth-AuthState";
    }

    public static class Routes
    {
        public const string LoginRedirect = "/__uauth/login-redirect";
    }
}
