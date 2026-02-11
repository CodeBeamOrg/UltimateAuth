using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Events;
using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Core.Runtime;
using CodeBeam.UltimateAuth.Credentials;
using CodeBeam.UltimateAuth.Policies.Abstractions;
using CodeBeam.UltimateAuth.Policies.Defaults;
using CodeBeam.UltimateAuth.Policies.Registry;
using CodeBeam.UltimateAuth.Server.Abstactions;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Runtime;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Server.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CodeBeam.UltimateAuth.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthServer(this IServiceCollection services, Action<UAuthServerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddUltimateAuth();

        AddUsersInternal(services);
        AddCredentialsInternal(services);
        AddAuthorizationInternal(services);
        AddUltimateAuthPolicies(services);

        services.AddOptions<UAuthServerOptions>()
            // Program.cs configuration (lowest precedence)
            .Configure(options =>
            {
                configure?.Invoke(options);
            })
            // appsettings.json (highest precedence)
            .BindConfiguration("UltimateAuth:Server")
            .PostConfigure(options =>
            {
                // Add any default values or adjustments here if needed
            });

        services.AddUltimateAuthServerInternal();

        return services;
    }

    private static IServiceCollection AddUltimateAuthServerInternal(this IServiceCollection services)
    {
        services.AddSingleton<IUAuthRuntimeMarker, ServerRuntimeMarker>();

        services.TryAddSingleton<IOpaqueTokenGenerator, OpaqueTokenGenerator>();
        services.TryAddSingleton<IJwtTokenGenerator,JwtTokenGenerator>();
        services.TryAddSingleton<IJwtSigningKeyProvider, DevelopmentJwtSigningKeyProvider>();

        services.TryAddScoped<ITokenHasher>(sp =>
        {
            var keyProvider = sp.GetRequiredService<IJwtSigningKeyProvider>();
            var key = keyProvider.Resolve(null);
            return new HmacSha256TokenHasher(((SymmetricSecurityKey)key.Key).Key);
        });

        // -----------------------------
        // OPTIONS VALIDATION
        // -----------------------------
        //services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<UAuthServerOptions>, UAuthServerOptionsValidator>());
        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerOptionsValidator>();
        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerLoginOptionsValidator>();
        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerSessionOptionsValidator>();
        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerTokenOptionsValidator>();
        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerMultiTenantOptionsValidator>();
        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerUserIdentifierOptionsValidator>();
        services.AddSingleton<IValidateOptions<UAuthServerOptions>, UAuthServerSessionResolutionOptionsValidator>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorityInvariant, DeviceRequiredInvariant>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorityInvariant, ExpiredSessionInvariant>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorityInvariant, InvalidOrRevokedSessionInvariant>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorityInvariant, TenantResolvedInvariant>());

        // EVENTS
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
            return options.Events.Clone();
        });

        services.AddSingleton<UAuthEventDispatcher>();

        // Tenant Resolution
        services.TryAddSingleton<ITenantIdResolver>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<UAuthMultiTenantOptions>>().Value;

            var resolvers = new List<ITenantIdResolver>();

            if (opts.EnableRoute)
                resolvers.Add(new PathTenantResolver());

            if (opts.EnableHeader)
                resolvers.Add(new HeaderTenantResolver(opts.HeaderName));

            if (opts.EnableDomain)
                resolvers.Add(new HostTenantResolver());

            return resolvers.Count switch
            {
                0 => new NullTenantResolver(),
                1 => resolvers[0],
                _ => new CompositeTenantResolver(resolvers)
            };
        });

        services.AddScoped<IInnerSessionIdResolver, CookieSessionIdResolver>();
        services.AddScoped<IInnerSessionIdResolver, BearerSessionIdResolver>();
        services.AddScoped<IInnerSessionIdResolver, HeaderSessionIdResolver>();
        services.AddScoped<IInnerSessionIdResolver, QuerySessionIdResolver>();

        services.TryAddScoped<ISessionIdResolver, CompositeSessionIdResolver>();

        services.TryAddScoped(typeof(IUAuthFlowService<>), typeof(UAuthFlowService<>));
        services.TryAddScoped<IRefreshFlowService, RefreshFlowService>();
        services.TryAddScoped<IUAuthSessionManager, UAuthSessionManager>();

        services.TryAddSingleton<IClock, SystemClock>();

        services.TryAddScoped<ISessionIssuer, UAuthSessionIssuer>();
        services.TryAddScoped<ITokenIssuer, UAuthTokenIssuer>();

        services.AddHttpContextAccessor();
        services.TryAddScoped(typeof(IUserAccessor<UserKey>), typeof(UAuthUserAccessor<UserKey>));
        services.TryAddScoped<IUserAccessor, UserAccessorBridge>();
        services.TryAddScoped<IAuthFlowContextAccessor, AuthFlowContextAccessor>();
        services.TryAddScoped<ISessionContextAccessor, SessionContextAccessor>();

        services.TryAddScoped<ISessionOrchestrator, UAuthSessionOrchestrator>();
        services.TryAddScoped(typeof(ILoginOrchestrator<>), typeof(LoginOrchestrator<>));
        services.TryAddScoped<IAccessOrchestrator, UAuthAccessOrchestrator>();
        services.TryAddScoped<ILoginAuthority, LoginAuthority>();
        services.TryAddScoped<IAuthAuthority, AuthAuthority>();
        services.TryAddScoped<IAccessAuthority, UAuthAccessAuthority>();

        services.TryAddScoped<IDeviceContextFactory, DeviceContextFactory>();
        services.TryAddScoped<IAuthContextFactory, AuthContextFactory>();
        services.TryAddScoped<IAuthFlowContextFactory, AuthFlowContextFactory>();
        services.TryAddScoped<IAccessContextFactory, AccessContextFactory>();

        services.AddSingleton<IClientBaseAddressProvider, OriginHeaderBaseAddressProvider>();
        services.AddSingleton<IClientBaseAddressProvider, RefererHeaderBaseAddressProvider>();
        services.AddSingleton<IClientBaseAddressProvider, ConfiguredClientBaseAddressProvider>();
        services.AddSingleton<IClientBaseAddressProvider, RequestHostBaseAddressProvider>();
        services.AddSingleton<ClientBaseAddressResolver>();

        services.TryAddScoped<ITenantResolver, UAuthTenantResolver>();
        services.TryAddScoped<IRefreshTokenResolver, RefreshTokenResolver>();
        services.TryAddScoped<IDeviceResolver, DeviceResolver>();
        services.TryAddScoped<IFlowCredentialResolver, FlowCredentialResolver>();
        services.TryAddScoped<ITransportCredentialResolver, TransportCredentialResolver>();
        services.TryAddScoped<IPrimaryCredentialResolver, PrimaryCredentialResolver>();
        services.TryAddSingleton<AuthResponseOptionsModeTemplateResolver>();
        services.TryAddSingleton<IAuthResponseResolver, AuthResponseResolver>();
        services.TryAddSingleton<IEffectiveAuthModeResolver, EffectiveAuthModeResolver>();
        services.TryAddSingleton<IPrimaryTokenResolver, PrimaryTokenResolver>();
        services.TryAddScoped<IHubCredentialResolver, HubCredentialResolver>();
        services.TryAddSingleton<IAuthRedirectResolver, AuthRedirectResolver>();

        services.TryAddScoped<ISessionTouchService, SessionTouchService>();
        services.TryAddScoped<ISessionQueryService, UAuthSessionQueryService>();
        services.TryAddScoped<IRefreshTokenRotationService, RefreshTokenRotationService>();

        services.TryAddScoped<ISessionValidator, UAuthSessionValidator>();
        services.TryAddScoped<IRefreshTokenValidator, UAuthRefreshTokenValidator>();
        services.TryAddScoped<IPkceAuthorizationValidator, PkceAuthorizationValidator>();

        services.TryAddScoped<ICredentialResponseWriter, CredentialResponseWriter>();
        services.TryAddScoped<IRefreshResponseWriter, RefreshResponseWriter>();
        services.TryAddSingleton<IClientProfileReader, ClientProfileReader>();
        services.TryAddScoped<IHubFlowReader, HubFlowReader>();
        
        services.TryAddSingleton<ClientProfileAuthResponseAdapter>();
        services.TryAddScoped<IEffectiveServerOptionsProvider,EffectiveServerOptionsProvider>();

        services.TryAddSingleton<IUAuthHeaderPolicyBuilder, UAuthHeaderPolicyBuilder>();
        services.TryAddSingleton<IUAuthBodyPolicyBuilder, UAuthBodyPolicyBuilder>();
        services.TryAddSingleton<IUAuthCookiePolicyBuilder, UAuthCookiePolicyBuilder>();
        services.TryAddScoped<IUAuthCookieManager, UAuthCookieManager>();

        services.TryAddScoped<IAuthFlow, AuthFlow>();
        services.TryAddScoped<IRefreshResponsePolicy, RefreshResponsePolicy>();
        services.TryAddSingleton<IAuthStore, InMemoryAuthStore>();
        services.TryAddScoped<ICurrentUser, HttpContextCurrentUser>();

        services.TryAddScoped<IHubCapabilities, HubCapabilities>();

        // -----------------------------
        // ENDPOINTS
        // -----------------------------
        services.TryAddScoped<AuthFlowEndpointFilter>();
        services.TryAddSingleton<IAuthEndpointRegistrar, UAuthEndpointRegistrar>();

        services.TryAddScoped<LoginEndpointHandler<UserKey>>();
        services.TryAddScoped<ILoginEndpointHandler, LoginEndpointHandlerBridge>();

        services.TryAddScoped<ValidateEndpointHandler>();
        services.TryAddScoped<IValidateEndpointHandler, ValidateEndpointHandlerBridge>();

        services.TryAddScoped<LogoutEndpointHandler<UserKey>>();
        services.TryAddScoped<ILogoutEndpointHandler, LogoutEndpointHandlerBridge>();

        services.TryAddScoped<RefreshEndpointHandler>();
        services.TryAddScoped<IRefreshEndpointHandler, RefreshEndpointHandlerBridge>();

        services.TryAddScoped<PkceEndpointHandler<UserKey>>();
        services.TryAddScoped<IPkceEndpointHandler, PkceEndpointHandlerBridge>();

        return services;
    }

    internal static IServiceCollection AddUltimateAuthPolicies(this IServiceCollection services, Action<AccessPolicyRegistry>? configure = null)
    {
        if (services.Any(d => d.ServiceType == typeof(AccessPolicyRegistry)))
            throw new InvalidOperationException("UltimateAuth policies already registered.");

        var registry = new AccessPolicyRegistry();

        DefaultPolicySet.Register(registry);
        configure?.Invoke(registry);

        services.AddSingleton(registry);

        services.AddSingleton(sp =>
        {
            var r = sp.GetRequiredService<AccessPolicyRegistry>();
            return r.Build();
        });

        services.AddScoped<IAccessPolicyProvider, AccessPolicyProvider>();

        services.TryAddScoped<IAccessAuthority>(sp =>
        {
            var invariants = sp.GetServices<IAccessInvariant>();
            var globalPolicies = sp.GetServices<IAccessPolicy>();
            return new UAuthAccessAuthority(invariants, globalPolicies);
        });

        return services;
    }

    // =========================
    // Users (Framework-Required)
    // =========================
    internal static IServiceCollection AddUsersInternal(IServiceCollection services)
    {
        services.TryAddScoped(typeof(IUserAccessor<UserKey>), typeof(UAuthUserAccessor<UserKey>));
        services.TryAddScoped<IUserAccessor, UserAccessorBridge>();
        return services;
    }

    // =========================
    // Credentials (Framework-Required)
    // =========================
    internal static IServiceCollection AddCredentialsInternal(IServiceCollection services)
    {
        services.TryAddScoped<ICredentialValidator, CredentialValidator>();
        return services;
    }

    // =========================
    // Authorization (Framework-Required)
    // =========================
    internal static IServiceCollection AddAuthorizationInternal(IServiceCollection services)
    {
        services.TryAddScoped(typeof(IUserClaimsProvider), typeof(AuthorizationClaimsProvider));
        return services;
    }


    public static IServiceCollection AddUAuthServerInfrastructure(this IServiceCollection services)
    {
        // Flow orchestration
        services.TryAddScoped(typeof(IUAuthFlowService<>), typeof(UAuthFlowService<>));

        // Issuers
        services.TryAddScoped(typeof(ISessionIssuer), typeof(UAuthSessionIssuer));
        services.TryAddScoped(typeof(ITokenIssuer), typeof(UAuthTokenIssuer));

        // Endpoints
        services.TryAddSingleton<IAuthEndpointRegistrar, UAuthEndpointRegistrar>();

        // Cookie management (default)
        services.TryAddSingleton<IUAuthCookieManager, UAuthCookieManager>();

        return services;
    }

}

internal sealed class NullTenantResolver : ITenantIdResolver
{
    public Task<string?> ResolveTenantIdAsync(TenantResolutionContext context) => Task.FromResult<string?>(null);
}
