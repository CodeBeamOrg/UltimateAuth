using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Server.Options
{
    /// <summary>
    /// Server-side configuration for UltimateAuth.
    /// Does NOT duplicate Core options.
    /// Instead, it composes SessionOptions, TokenOptions, PkceOptions, MultiTenantOptions
    /// and adds server-only behaviors (routing, endpoint activation, policies).
    /// </summary>
    public sealed class UAuthServerOptions
    {
        public UAuthClientProfile ClientProfile { get; set; }

        /// <summary>
        /// Defines how UltimateAuth executes authentication flows.
        /// Default is Hybrid.
        /// </summary>
        public UAuthMode? Mode { get; set; }

        /// <summary>
        /// Defines how UAuthHub is deployed relative to the application.
        /// Default is Integrated
        /// Blazor server projects should choose embedded mode for maximum security.
        /// </summary>
        public UAuthHubDeploymentMode HubDeploymentMode { get; set; } = UAuthHubDeploymentMode.Integrated;

        // -------------------------------------------------------
        // ROUTING
        // -------------------------------------------------------

        /// <summary>
        /// Base API route. Default: "/auth"
        /// Changing this prevents conflicts with other auth systems.
        /// </summary>
        public string RoutePrefix { get; set; } = "/auth";


        // -------------------------------------------------------
        // CORE OPTION COMPOSITION
        // (Server must NOT duplicate Core options)
        // -------------------------------------------------------

        /// <summary>
        /// Session behavior (lifetime, sliding expiration, etc.)
        /// Fully defined in Core.
        /// </summary>
        public UAuthSessionOptions Session { get; set; } = new();

        /// <summary>
        /// Token issuing behavior (lifetimes, refresh policies).
        /// Fully defined in Core.
        /// </summary>
        public UAuthTokenOptions Tokens { get; set; } = new();

        /// <summary>
        /// PKCE configuration (required for WASM).
        /// Fully defined in Core.
        /// </summary>
        public UAuthPkceOptions Pkce { get; set; } = new();

        /// <summary>
        /// Multi-tenancy behavior (resolver, normalization, etc.)
        /// Fully defined in Core.
        /// </summary>
        public UAuthMultiTenantOptions MultiTenant { get; set; } = new();

        /// <summary>
        /// Allows advanced users to override cookie behavior.
        /// Unsafe combinations will be rejected at startup.
        /// </summary>
        public UAuthCookieOptions Cookie { get; } = new();

        internal Type? CustomCookieManagerType { get; private set; }

        public void ReplaceSessionCookieManager<T>() where T : class, IUAuthSessionCookieManager
        {
            CustomCookieManagerType = typeof(T);
        }

        // -------------------------------------------------------
        // SERVER-ONLY BEHAVIOR
        // -------------------------------------------------------

        /// <summary>
        /// Enables/disables specific endpoint groups.
        /// Useful for API hardening.
        /// </summary>
        public bool? EnableLoginEndpoints { get; set; } = true;
        public bool? EnablePkceEndpoints { get; set; } = true;
        public bool? EnableTokenEndpoints { get; set; } = true;
        public bool? EnableSessionEndpoints { get; set; } = true;
        public bool? EnableUserInfoEndpoints { get; set; } = true;

        /// <summary>
        /// If true, server will add anti-forgery headers
        /// and require proper request metadata.
        /// </summary>
        public bool EnableAntiCsrfProtection { get; set; } = true;

        /// <summary>
        /// If true, login attempts are rate-limited to prevent brute force attacks.
        /// </summary>
        public bool EnableLoginRateLimiting { get; set; } = true;


        // -------------------------------------------------------
        // CUSTOMIZATION HOOKS
        // -------------------------------------------------------

        /// <summary>
        /// Allows developers to mutate endpoint routing AFTER UltimateAuth registers defaults.
        /// Example: adding new routes, overriding authorization, adding filters.
        /// </summary>
        public Action<WebApplication>? OnConfigureEndpoints { get; set; }

        /// <summary>
        /// Allows developers to add or replace server services before DI is built.
        /// Example: overriding default ILoginService.
        /// </summary>
        public Action<IServiceCollection>? ConfigureServices { get; set; }
    }
}
