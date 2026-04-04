namespace CodeBeam.UltimateAuth.Client.Options;

public sealed class UAuthClientLoginFlowOptions
{
    /// <summary>
    /// Default return URL after a successful login flow.
    /// If not set, global default return url or current location will be used.
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Allows posting credentials (e.g. username/password) directly to the server.
    /// 
    /// ⚠️ SECURITY WARNING:
    /// This MUST NOT be enabled for public clients (e.g. Blazor WASM, SPA).
    /// Public clients are required to use PKCE-based login flows.
    ///
    /// Enable this option ONLY for trusted server-hosted clients
    /// such as Blazor Server or UAuthHub.
    ///
    /// This option may be temporarily enabled for debugging purposes,
    /// but doing so is inherently insecure and MUST NOT be used in production.
    /// </summary>
    public bool AllowCredentialPost { get; set; } = false;
}
