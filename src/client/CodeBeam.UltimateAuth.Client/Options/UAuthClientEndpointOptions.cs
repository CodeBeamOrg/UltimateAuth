namespace CodeBeam.UltimateAuth.Client.Options;

/// <summary>
/// Options for configuring the endpoints of the UAuth client.
/// </summary>
public sealed class UAuthClientEndpointOptions
{
    /// <summary>
    /// Base URL of UAuthHub (e.g. https://localhost:6110)
    /// </summary>
    public string BasePath { get; set; } = "/auth";

    /// <summary>
    /// Path for the login endpoint (e.g. /login)
    /// </summary>
    public string Login { get; set; } = "/login";

    /// <summary>
    /// Path for the try login endpoint (e.g. /try-login)
    /// </summary>
    public string TryLogin { get; set; } = "/try-login";

    /// <summary>
    /// Path for the logout endpoint (e.g. /logout)
    /// </summary>
    public string Logout { get; set; } = "/logout";

    /// <summary>
    /// Path for the refresh endpoint (e.g. /refresh)
    /// </summary>
    public string Refresh { get; set; } = "/refresh";

    /// <summary>
    /// Path for the reauth endpoint (e.g. /reauth)
    /// </summary>
    public string Reauth { get; set; } = "/reauth";

    /// <summary>
    /// Path for the validate endpoint (e.g. /validate)
    /// </summary>
    public string Validate { get; set; } = "/validate";

    /// <summary>
    /// Path for the PKCE authorize endpoint (e.g. /pkce/authorize)
    /// </summary>
    public string PkceAuthorize { get; set; } = "/pkce/authorize";

    /// <summary>
    /// Path for the PKCE try complete endpoint (e.g. /pkce/try-complete)
    /// </summary>
    public string PkceTryComplete { get; set; } = "/pkce/try-complete";

    /// <summary>
    /// Path for the PKCE complete endpoint (e.g. /pkce/complete)
    /// </summary>
    public string PkceComplete { get; set; } = "/pkce/complete";

    /// <summary>
    /// Path for the UAuthHub login endpoint (e.g. /uauthhub/entry)
    /// </summary>
    public string HubLoginPath { get; set; } = "/uauthhub/entry";
}
