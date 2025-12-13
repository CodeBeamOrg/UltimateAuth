namespace CodeBeam.UltimateAuth.Server.Options
{
    public sealed class UAuthSessionResolutionOptions
    {
        public bool EnableBearer { get; set; } = true;
        public bool EnableHeader { get; set; } = true;
        public bool EnableCookie { get; set; } = true;
        public bool EnableQuery { get; set; } = false;

        public string HeaderName { get; set; } = "X-UAuth-Session";
        public string CookieName { get; set; } = "__uauth";
        public string QueryParameterName { get; set; } = "session_id";

        // Precedence order
        // Example: Bearer, Header, Cookie, Query
        public List<string> Order { get; set; } = new()
        {
            "Bearer",
            "Header",
            "Cookie",
            "Query"
        };
    }
}
