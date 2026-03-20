namespace CodeBeam.UltimateAuth.Client.Options;

public sealed class UAuthClientEndpointOptions
{
    /// <summary>
    /// Base URL of UAuthHub (e.g. https://localhost:6110)
    /// </summary>
    public string BasePath { get; set; } = "/auth";

    public string Login { get; set; } = "/login";
    public string Logout { get; set; } = "/logout";
    public string Refresh { get; set; } = "/refresh";
    public string Reauth { get; set; } = "/reauth";
    public string Validate { get; set; } = "/validate";
    public string PkceAuthorize { get; set; } = "/pkce/authorize";
    public string PkceComplete { get; set; } = "/pkce/complete";
    public string HubLoginPath { get; set; } = "/uauthhub/login";
}
