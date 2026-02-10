using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Client.Options;

public sealed class UAuthClientOptions
{
    public UAuthClientProfile ClientProfile { get; set; } = UAuthClientProfile.NotSpecified;
    public bool AutoDetectClientProfile { get; set; } = true;

    /// <summary>
    /// Global fallback return URL used by interactive authentication flows
    /// when no flow-specific return URL is provided.
    /// </summary>
    public string? DefaultReturnUrl { get; set; }


    public UAuthClientEndpointOptions Endpoints { get; set; } = new();
    public UAuthClientLoginFlowOptions Login { get; set; } = new();

    /// <summary>
    /// Options related to PKCE-based login flows.
    /// </summary>
    public UAuthClientPkceLoginFlowOptions Pkce { get; set; } = new();
    public UAuthClientRefreshOptions Refresh { get; set; } = new();
    public ReauthOptions Reauth { get; init; } = new();
    public UAuthClientMultiTenantOptions MultiTenant { get; set; } = new();
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
