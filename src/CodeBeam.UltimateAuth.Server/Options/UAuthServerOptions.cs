using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Events;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Credentials.Options;
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

    /// <summary>
    /// Controls the default and maximum page sizes for paginated queries.
    /// </summary>
    public UAuthPaginationOptions Pagination { get; set; } = new();

    // -------------------------------------------------------
    // SERVER-ONLY BEHAVIOR
    // -------------------------------------------------------

    /// <summary>
    /// Gets or sets the authentication mode used for all client profiles when explicitly configured.
    /// </summary>
    /// <remarks>
    /// When null, mode selection uses the configured client profile mappings or the registered authentication mode resolver.
    /// <br/><br/>
    /// Setting this property establishes a server-controlled mode selection that cannot be changed by client profile headers.
    /// <br/><br/>
    /// This setting is mutually exclusive with ClientProfileModes.
    /// The selected mode must be included in AllowedModes when an allowlist is configured.
    /// </remarks>
    public UAuthMode? AuthMode { get; set; }

    /// <summary>
    /// Gets or sets explicit authentication mode mappings for individual client profiles.
    /// </summary>
    /// <remarks>
    /// Mappings override the default automatic selection for the specified profiles.
    /// Profiles without mappings use the registered resolver.
    /// <br/><br/>
    /// Client profiles describe client environments and are not authenticated client identities.
    /// Use a server-controlled authentication policy when selecting modes is security-sensitive.
    /// <br/><br/>
    /// Cannot be combined with an explicit AuthMode.
    /// </remarks>
    public Dictionary<UAuthClientProfile, UAuthMode> ClientProfileModes { get; set; } = new();

    /// <summary>
    /// Cryptographic keys used by the built-in implementations.
    /// </summary>
    public UAuthCryptographyOptions Cryptography { get; set; } = new();

    /// <summary>
    /// Startup-only native endpoint throttling; application policies take precedence.
    /// </summary>
    public UAuthRateLimitingOptions RateLimiting { get; set; } = new();

    public UAuthConcurrencyLimitingOptions ConcurrencyLimiting { get; set; } = new();

    /// <summary>
    /// Controls optional global and per-type active device limits.
    /// No limits by default.
    /// </summary>
    public UAuthDeviceLimitOptions DeviceLimits { get; set; } = new();

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

    /// <summary>
    /// Configures how authentication grants are delivered
    /// to clients.
    /// </summary>
    public UAuthGrantDeliveryOptions GrantDelivery { get; set; } = new();

    /// <summary>
    /// Configures authentication flow redirects.
    /// </summary>
    public UAuthRedirectOptions Redirect { get; set; } = new();

    public UAuthResetOptions ResetCredential { get; init; } = new();

    /// <summary>
    /// Controls session invalidation after credential changes and recovery.
    /// </summary>
    public UAuthCredentialSessionPolicyOptions CredentialSessionPolicy { get; init; } = new();

    /// <summary>
    /// Controls password strength, common-password detection,
    /// user similarity checks, and password reuse history.
    /// </summary>
    public PasswordPolicyOptions Password { get; set; } = new();

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

    public UAuthUserProfileOptions UserProfile { get; set; } = new();

    public UAuthNavigationOptions Navigation { get; set; } = new();

    public UAuthAntiforgeryOptions Antiforgery { get; set; } = new();

    /// <summary>
    /// Capacity and expiration cleanup for the built-in auth artifact store.
    /// </summary>
    public UAuthArtifactStoreOptions AuthArtifacts { get; set; } = new();

    [Obsolete("AuthResponse is no longer supported. Use GrantDelivery and Redirect instead.")]
    public UAuthResponseOptions AuthResponse
    {
        get => throw new NotSupportedException(
            "AuthResponse is no longer supported. " +
            "Use GrantDelivery and Redirect instead.");

        set => throw new NotSupportedException(
            "AuthResponse is no longer supported. " +
            "Use GrantDelivery and Redirect instead.");
    }


    // -------------------------------------------------------
    // CUSTOMIZATION HOOKS
    // -------------------------------------------------------

    /// <summary>
    /// Allows developers to mutate endpoint routing AFTER UltimateAuth registers defaults like
    /// adding new routes, overriding authorization, adding filters.
    /// This hook must not remove or re-map UltimateAuth endpoints. Misuse may break security guarantees.
    /// </summary>
    public Action<IEndpointRouteBuilder>? OnConfigureEndpoints { get; set; }

    // TODO: Add same startup validations. Mode-based options should not pass through the option invariant.
    internal Dictionary<UAuthMode, Action<UAuthServerOptions>> ModeConfigurations { get; set; } = new();


    internal UAuthServerOptions Clone()
    {
        return new UAuthServerOptions
        {
            AuthMode = AuthMode,
            ClientProfileModes = new(ClientProfileModes),
            Cryptography = Cryptography.Clone(),
            AllowedModes = AllowedModes?.ToArray(),
            HubDeploymentMode = HubDeploymentMode,

            RateLimiting = RateLimiting.Clone(),
            ConcurrencyLimiting = ConcurrencyLimiting.Clone(),
            DeviceLimits = DeviceLimits.Clone(),
            Login = Login.Clone(),
            Session = Session.Clone(),
            Token = Token.Clone(),
            Pkce = Pkce.Clone(),
            Events = Events.Clone(),
            MultiTenant = MultiTenant.Clone(),
            Pagination = Pagination.Clone(),

            Cookie = Cookie.Clone(),
            Diagnostics = Diagnostics.Clone(),
            PrimaryCredential = PrimaryCredential.Clone(),
            GrantDelivery = GrantDelivery.Clone(),
            Redirect = Redirect.Clone(),
            ResetCredential = ResetCredential.Clone(),
            CredentialSessionPolicy = CredentialSessionPolicy.Clone(),
            Password = Password.Clone(),
            Hub = Hub.Clone(),
            SessionResolution = SessionResolution.Clone(),
            Identifiers = Identifiers.Clone(),
            IdentifierValidation = IdentifierValidation.Clone(),
            LoginIdentifiers = LoginIdentifiers.Clone(),
            UserProfile = UserProfile.Clone(),
            Endpoints = Endpoints.Clone(),
            Navigation = Navigation.Clone(),
            Antiforgery = Antiforgery.Clone(),
            AuthArtifacts = AuthArtifacts.Clone(),

            ModeConfigurations = new Dictionary<UAuthMode, Action<UAuthServerOptions>>(ModeConfigurations),
            OnConfigureEndpoints = OnConfigureEndpoints,
        };
    }
}
