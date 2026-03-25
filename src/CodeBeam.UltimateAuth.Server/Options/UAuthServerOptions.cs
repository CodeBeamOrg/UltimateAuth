using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Events;
using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.AspNetCore.Routing;

namespace CodeBeam.UltimateAuth.Server.Options;

/// <summary>
/// Server-side configuration for UltimateAuth.
/// Does NOT duplicate Core options.
/// Instead, it composes SessionOptions, TokenOptions, PkceOptions, MultiTenantOptions
/// and adds server-only behaviors (routing, endpoint activation, policies).
/// </summary>
public sealed class UAuthServerOptions
{

    // -------------------------------------------------------
    // CORE OPTION COMPOSITION
    // (Server must NOT duplicate Core options)
    // -------------------------------------------------------

    public UAuthLoginOptions Login { get; set; } = new();

    /// <summary>
    /// Session behavior (lifetime, sliding expiration, etc.) Fully defined in Core.
    /// </summary>
    public UAuthSessionOptions Session { get; set; } = new();

    /// <summary>
    /// Token issuing behavior (lifetimes, refresh policies). Fully defined in Core.
    /// </summary>
    public UAuthTokenOptions Token { get; set; } = new();

    /// <summary>
    /// PKCE configuration (required for WASM). Fully defined in Core.
    /// </summary>
    public UAuthPkceOptions Pkce { get; set; } = new();

    public UAuthEvents Events { get; set; } = new();

    /// <summary>
    /// Multi-tenancy behavior (resolver, normalization, etc.) Fully defined in Core.
    /// </summary>
    public UAuthMultiTenantOptions MultiTenant { get; set; } = new();

    // -------------------------------------------------------
    // SERVER-ONLY BEHAVIOR
    // -------------------------------------------------------

    /// <summary>
    /// Defines which authentication modes are allowed to be used by the server.
    /// This is a safety guardrail, not a mode selection mechanism.
    /// The final mode is still resolved via IEffectiveAuthModeResolver.
    /// If null or empty, all modes are allowed.
    /// </summary>
    public IReadOnlyCollection<UAuthMode>? AllowedModes { get; set; }

    /// <summary>
    /// Defines how UAuthHub is deployed relative to the application.
    /// Default is Integrated
    /// Blazor server projects should choose embedded mode for maximum security.
    /// </summary>
    public UAuthHubDeploymentMode HubDeploymentMode { get; set; } = UAuthHubDeploymentMode.Integrated;

    /// <summary>
    /// Allows advanced users to override cookie behavior.
    /// Unsafe combinations will be rejected at startup.
    /// </summary>
    public UAuthCookiePolicyOptions Cookie { get; set; } = new();

    public UAuthDiagnosticsOptions Diagnostics { get; set; } = new();

    public UAuthPrimaryCredentialPolicy PrimaryCredential { get; init; } = new();

    public UAuthResponseOptions AuthResponse { get; init; } = new();

    public UAuthResetOptions ResetCredential { get; init; } = new();

    public UAuthHubOptions Hub { get; set; } = new();

    /// <summary>
    /// Controls how session identifiers are resolved from incoming requests
    /// (cookie, header, bearer, query, order, etc.)
    /// </summary>
    public UAuthSessionResolutionOptions SessionResolution { get; set; } = new();

    /// <summary>
    /// Enables/disables specific endpoint groups. Useful for API hardening.
    /// </summary>
    public UAuthServerEndpointOptions Endpoints { get; set; } = new();

    public UAuthIdentifierOptions Identifiers { get; set; } = new();

    public UAuthIdentifierValidationOptions IdentifierValidation { get; set; } = new();

    public UAuthLoginIdentifierOptions LoginIdentifiers { get; set; } = new();

    public UAuthNavigationOptions Navigation { get; set; } = new();


    ///// <summary>
    ///// If true, server will add anti-forgery headers
    ///// and require proper request metadata.
    ///// </summary>
    //public bool EnableAntiCsrfProtection { get; set; } = true;

    ///// <summary>
    ///// If true, login attempts are rate-limited to prevent brute force attacks.
    ///// </summary>
    //public bool EnableLoginRateLimiting { get; set; } = true;


    // -------------------------------------------------------
    // CUSTOMIZATION HOOKS
    // -------------------------------------------------------

    /// <summary>
    /// Allows developers to mutate endpoint routing AFTER UltimateAuth registers defaults like
    /// adding new routes, overriding authorization, adding filters.
    /// This hook must not remove or re-map UltimateAuth endpoints. Misuse may break security guarantees.
    /// </summary>
    public Action<IEndpointRouteBuilder>? OnConfigureEndpoints { get; set; }

    internal Dictionary<UAuthMode, Action<UAuthServerOptions>> ModeConfigurations { get; set; } = new();


    internal UAuthServerOptions Clone()
    {
        return new UAuthServerOptions
        {
            AllowedModes = AllowedModes?.ToArray(),
            HubDeploymentMode = HubDeploymentMode,

            Login = Login.Clone(),
            Session = Session.Clone(),
            Token = Token.Clone(),
            Pkce = Pkce.Clone(),
            Events = Events.Clone(),
            MultiTenant = MultiTenant.Clone(),
            Cookie = Cookie.Clone(),
            Diagnostics = Diagnostics.Clone(),

            PrimaryCredential = PrimaryCredential.Clone(),
            AuthResponse = AuthResponse.Clone(),
            ResetCredential = ResetCredential.Clone(),
            Hub = Hub.Clone(),
            SessionResolution = SessionResolution.Clone(),
            Identifiers = Identifiers.Clone(),
            IdentifierValidation = IdentifierValidation.Clone(),
            LoginIdentifiers = LoginIdentifiers.Clone(),
            Endpoints = Endpoints.Clone(),
            Navigation = Navigation.Clone(),

            //EnableAntiCsrfProtection = EnableAntiCsrfProtection,
            //EnableLoginRateLimiting = EnableLoginRateLimiting,

            ModeConfigurations = new Dictionary<UAuthMode, Action<UAuthServerOptions>>(ModeConfigurations),

            OnConfigureEndpoints = OnConfigureEndpoints,
        };
    }
}
