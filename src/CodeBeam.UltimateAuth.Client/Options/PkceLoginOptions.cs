using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Client.Options;

public sealed class PkceLoginOptions
{
    /// <summary>
    /// Enables PKCE login support.
    /// </summary>
    public bool Enabled { get; set; } = true;

    public string? ReturnUrl { get; init; }

    /// <summary>
    /// Called after authorization_code is issued,
    /// before redirecting to the Hub.
    /// </summary>
    public Func<PkceAuthorizeResponse, Task>? OnAuthorized { get; init; }

    /// <summary>
    /// If false, BeginPkceAsync will NOT redirect automatically.
    /// Caller is responsible for navigation.
    /// </summary>
    public bool AutoRedirect { get; init; } = true;
}
