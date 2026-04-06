using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Client.Options;

/// <summary>
/// Represents client-side configuration for UltimateAuth.
/// </summary>
/// <remarks>
/// <para>
/// This class defines how the client application interacts with the UltimateAuth server,
/// including authentication flows, endpoints, client behavior, and multi-tenant support.
/// </para>
///
/// <para>
/// The most important concept is <see cref="ClientProfile"/>, which determines
/// how authentication behaves depending on the client type (e.g., Blazor Server, WASM, API).
/// </para>
///
/// <para>
/// Key areas:
/// <list type="bullet">
/// <item><description><b>Client Profile:</b> Controls auth mode (session vs token, PKCE, etc.)</description></item>
/// <item><description><b>Flows:</b> Login, PKCE, refresh, and reauthentication behavior</description></item>
/// <item><description><b>Endpoints:</b> Server route configuration</description></item>
/// <item><description><b>State Events:</b> Client-side state change notifications</description></item>
/// <item><description><b>Multi-Tenancy:</b> Tenant resolution and propagation</description></item>
/// </list>
/// </para>
///
/// <para>
/// Important:
/// <list type="bullet">
/// <item><description>If <see cref="AutoDetectClientProfile"/> is enabled, the profile is inferred automatically.</description></item>
/// <item><description>If disabled, <see cref="ClientProfile"/> must be explicitly set.</description></item>
/// <item><description>Different profiles may result in different authentication modes (e.g., PureOpaque vs Hybrid).</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class UAuthClientOptions
{
    /// <summary>
    /// Specifies the client profile used for authentication behavior.
    /// </summary>
    /// <remarks>
    /// Determines how authentication flows are executed (e.g., session-based, PKCE, token-based).
    /// </remarks>
    public UAuthClientProfile ClientProfile { get; set; } = UAuthClientProfile.NotSpecified;

    /// <summary>
    /// Enables automatic detection of the client profile.
    /// </summary>
    /// <remarks>
    /// When enabled, UltimateAuth infers the client type (e.g., Blazor Server, WASM).
    /// Disable this to guarantee explicitly control behavior via <see cref="ClientProfile"/>.
    /// </remarks>
    public bool AutoDetectClientProfile { get; set; } = true;

    /// <summary>
    /// Default return URL used for interactive authentication flows.
    /// </summary>
    /// <remarks>
    /// Used when no return URL is explicitly provided in login or PKCE flows.
    /// </remarks>
    public string? DefaultReturnUrl { get; set; }

    /// <summary>
    /// Configures client-side state change events.
    /// </summary>
    /// <remarks>
    /// Controls how authentication-related events (e.g., login, logout, profile changes) are propagated and handled within the client.
    /// </remarks>
    public UAuthStateEventOptions StateEvents { get; set; } = new();

    /// <summary>
    /// Defines server endpoint paths used by the client.
    /// </summary>
    /// <remarks>
    /// Allows customization of API routes for authentication and user operations.
    /// </remarks>
    public UAuthClientEndpointOptions Endpoints { get; set; } = new();

    /// <summary>
    /// Options related to login flow behavior.
    /// </summary>
    /// <remarks>
    /// Controls how login requests are executed and handled on the client.
    /// </remarks>
    public UAuthClientLoginFlowOptions Login { get; set; } = new();

    /// <summary>
    /// Options related to PKCE-based login flows.
    /// </summary>
    public UAuthClientPkceLoginFlowOptions Pkce { get; set; } = new();

    /// <summary>
    /// Configures automatic session/token refresh behavior.
    /// </summary>
    /// <remarks>
    /// Determines how and when refresh operations are triggered.
    /// </remarks>
    public UAuthClientAutoRefreshOptions AutoRefresh { get; set; } = new();

    /// <summary>
    /// Options for reauthentication behavior.
    /// </summary>
    /// <remarks>
    /// Used when session becomes invalid and user interaction is required again.
    /// </remarks>
    public UAuthClientReauthOptions Reauth { get; init; } = new();

    /// <summary>
    /// Configures multi-tenant behavior for the client.
    /// </summary>
    /// <remarks>
    /// Controls how tenant information is resolved and included in requests.
    /// </remarks>
    public UAuthClientMultiTenantOptions MultiTenant { get; set; } = new();
}
