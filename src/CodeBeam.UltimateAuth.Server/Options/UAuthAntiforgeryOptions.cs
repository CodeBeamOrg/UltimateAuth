namespace CodeBeam.UltimateAuth.Server.Options;

/// <summary>
/// Configures CSRF protection for UltimateAuth endpoints.
/// </summary>
public sealed class UAuthAntiforgeryOptions
{
    // TODO: Implement form field and header name configuration for antiforgery token submission. Check uauth.js file carefully to ensure that the form field and header names are consistent with the client-side implementation.

    /// <summary>
    /// Path segment used to retrieve a request token.
    /// Resolved under the configured UltimateAuth base path.
    /// </summary>
    public string TokenEndpoint { get; set; } = "/antiforgery/token";

    public UAuthAntiforgeryOptions Clone() => new()
    {
        TokenEndpoint = TokenEndpoint
    };
}
