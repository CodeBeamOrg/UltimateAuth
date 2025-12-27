namespace CodeBeam.UltimateAuth.Client.Options
{
    public sealed class UAuthClientOptions
    {
        public AuthEndpointOptions Endpoints { get; set; } = new();
    }

    public sealed class AuthEndpointOptions
    {
        public string Login { get; set; } = "/auth/login";
        public string Logout { get; set; } = "/auth/logout";
        public string Refresh { get; set; } = "/auth/refresh";
        public string Reauth { get; set; } = "/auth/reauth";
    }
}
