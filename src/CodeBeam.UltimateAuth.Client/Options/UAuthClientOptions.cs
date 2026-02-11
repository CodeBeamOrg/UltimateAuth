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
    public UAuthClientAutoRefreshOptions AutoRefresh { get; set; } = new();
    public UAuthClientReauthOptions Reauth { get; init; } = new();
    public UAuthClientMultiTenantOptions MultiTenant { get; set; } = new();
}
