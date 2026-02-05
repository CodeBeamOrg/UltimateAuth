using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Options;

public sealed class UAuthClientOptions
{
    public AuthEndpointOptions Endpoints { get; set; } = new();
    public LoginFlowOptions Login { get; set; } = new();

    /// <summary>
    /// Options related to PKCE-based login flows.
    /// </summary>
    public PkceLoginOptions Pkce { get; set; } = new();
    public UAuthClientRefreshOptions Refresh { get; set; } = new();
    public ReauthOptions Reauth { get; init; } = new();
}

public sealed class AuthEndpointOptions
{
    /// <summary>
    /// Base URL of UAuthHub (e.g. https://localhost:6110)
    /// </summary>
    public string Authority { get; set; } = "/auth";

    public string Login { get; set; } = "/login";
    public string Logout { get; set; } = "/logout";
    public string Refresh { get; set; } = "/refresh";
    public string Reauth { get; set; } = "/reauth";
    public string Validate { get; set; } = "/validate";
    public string PkceAuthorize { get; set; } = "/pkce/authorize";
    public string PkceComplete { get; set; } = "/pkce/complete";
    public string HubLoginPath { get; set; } = "/uauthhub/login";
}

public sealed class LoginFlowOptions
{
    /// <summary>
    /// Default return URL after a successful login flow.
    /// If not set, current location will be used.
    /// </summary>
    public string? DefaultReturnUrl { get; set; }

    /// <summary>
    /// Enables or disables direct credential-based login.
    /// </summary>
    public bool AllowDirectLogin { get; set; } = true;
}

public sealed class UAuthClientRefreshOptions
{
    /// <summary>
    /// Enables background refresh coordination.
    /// Default: true for BlazorServer, false otherwise.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval for background refresh attempts.
    /// This is a UX / keep-alive setting, NOT a security policy.
    /// </summary>
    public TimeSpan? Interval { get; set; }

    /// <summary>
    /// Optional jitter to avoid synchronized refresh storms.
    /// </summary>
    public TimeSpan? Jitter { get; set; }
}

// TODO: Add ClearCookieOnReauth
public sealed class ReauthOptions
{
    public ReauthBehavior Behavior { get; set; } = ReauthBehavior.RedirectToLogin;
    public string LoginPath { get; set; } = "/login";
}
