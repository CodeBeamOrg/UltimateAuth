using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Client.Options;

/// <summary>
/// Options for configuring the PKCE login flow in the UAuth client.
/// </summary>
public sealed class UAuthClientPkceLoginFlowOptions
{
    /// <summary>
    /// Enables PKCE login support.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The URL to redirect to after successful login.
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Called after authorization_code is issued,
    /// before redirecting to the Hub.
    /// </summary>
    public Func<PkceAuthorizeResponse, Task>? OnAuthorized { get; set; }

    /// <summary>
    /// If false, BeginPkceAsync will NOT redirect automatically.
    /// Caller is responsible for navigation.
    /// </summary>
    public bool AutoRedirect { get; set; } = true;
}
