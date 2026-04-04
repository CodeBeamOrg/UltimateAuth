namespace CodeBeam.UltimateAuth.Server.Options;

// TODO: Check header/query parameter name conflicts with other auth mechanisms (e.g. API keys, OAuth tokens)
// We removed CookieName here because cookie-based session resolution, there may be other conflicts.
public sealed class UAuthSessionResolutionOptions
{
    public bool EnableBearer { get; set; } = true;
    public bool EnableHeader { get; set; } = true;
    public bool EnableCookie { get; set; } = true;
    public bool EnableQuery { get; set; } = true;

    public string HeaderName { get; set; } = "X-UAuth-Session";
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

    internal UAuthSessionResolutionOptions Clone() => new()
    {
        EnableBearer = EnableBearer,
        EnableHeader = EnableHeader,
        EnableCookie = EnableCookie,
        EnableQuery = EnableQuery,
        HeaderName = HeaderName,
        QueryParameterName = QueryParameterName,
        Order = new List<string>(Order)
    };
}
