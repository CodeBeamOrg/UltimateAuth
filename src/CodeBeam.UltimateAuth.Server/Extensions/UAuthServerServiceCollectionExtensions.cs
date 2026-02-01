using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Credentials;
using CodeBeam.UltimateAuth.Policies.Abstractions;
using CodeBeam.UltimateAuth.Policies.Defaults;
using CodeBeam.UltimateAuth.Policies.Registry;
using CodeBeam.UltimateAuth.Server.Abstactions;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Cookies;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Infrastructure.Hub;
using CodeBeam.UltimateAuth.Server.Infrastructure.Session;
using CodeBeam.UltimateAuth.Server.Issuers;
using CodeBeam.UltimateAuth.Server.Login;
using CodeBeam.UltimateAuth.Server.Login.Orchestrators;
using CodeBeam.UltimateAuth.Server.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Server.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CodeBeam.UltimateAuth.Server.Extensions
{
    public static class UAuthServerServiceCollectionExtensions
    {
        public static IServiceCollection AddUltimateAuthServer(this IServiceCollection services)
        {
            services.AddUltimateAuth();
            AddUsersInternal(services);
            AddCredentialsInternal(services);
            AddUltimateAuthPolicies(services);
            return services.AddUltimateAuthServerInternal();
        }

        public static IServiceCollection AddUltimateAuthServer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddUltimateAuth(configuration);
            AddUsersInternal(services);
            AddCredentialsInternal(services);
            AddUltimateAuthPolicies(services);
            services.Configure<UAuthServerOptions>(configuration.GetSection("UltimateAuth:Server"));

            return services.AddUltimateAuthServerInternal();
        }

        public static IServiceCollection AddUltimateAuthServer(this IServiceCollection services, Action<UAuthServerOptions> configure)
        {
            services.AddUltimateAuth();
            AddUsersInternal(services);
            AddCredentialsInternal(services);
            AddUltimateAuthPolicies(services);
            services.Configure(configure);

            return services.AddUltimateAuthServerInternal();
        }

        private static IServiceCollection AddUltimateAuthServerInternal(this IServiceCollection services)
        {
            //services.AddSingleton<IServerProfileDetector, UAuthServerProfileDetector>();
            //services.PostConfigure<UAuthOptions>(o =>
            //{
            //    if (!o.AutoDetectClientProfile || o.ClientProfile != UAuthClientProfile.NotSpecified)
            //        return;

            //    using var sp = services.BuildServiceProvider();
            //    var detector = sp.GetRequiredService<IServerProfileDetector>();
            //    o.ClientProfile = detector.Detect(sp);
            //});

            //services.AddOptions<UAuthServerOptions>()
            //    .PostConfigure<IOptions<UAuthOptions>>((server, core) =>
            //    {
            //        ConfigureDefaults.ApplyClientProfileDefaults(server, core.Value);
            //        ConfigureDefaults.ApplyModeDefaults(server);
            //        ConfigureDefaults.ApplyAuthResponseDefaults(server, core.Value);
            //    });

            services.TryAddSingleton<IOpaqueTokenGenerator, DefaultOpaqueTokenGenerator>();
            services.TryAddSingleton<IJwtTokenGenerator,DefaultJwtTokenGenerator>();
            services.TryAddSingleton<IJwtSigningKeyProvider, DevelopmentJwtSigningKeyProvider>();

            services.TryAddSingleton<ITokenHasher>(sp =>
            {
                var keyProvider = sp.GetRequiredService<IJwtSigningKeyProvider>();
                var key = keyProvider.Resolve(null);

                return new HmacSha256TokenHasher(
                    ((SymmetricSecurityKey)key.Key).Key);
            });


            // -----------------------------
            // OPTIONS VALIDATION
            // -----------------------------
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<UAuthServerOptions>, UAuthServerOptionsValidator>());

            // -----------------------------
            // TENANT RESOLUTION
            // -----------------------------
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
                    0 => new FixedTenantResolver(opts.DefaultTenantId ?? "default"),
                    1 => resolvers[0],
                    _ => new CompositeTenantResolver(resolvers)
                };
            });

            // Inner resolvers
            services.AddScoped<IInnerSessionIdResolver, CookieSessionIdResolver>();
            services.AddScoped<IInnerSessionIdResolver, BearerSessionIdResolver>();
            services.AddScoped<IInnerSessionIdResolver, HeaderSessionIdResolver>();
            services.AddScoped<IInnerSessionIdResolver, QuerySessionIdResolver>();

            // Public resolver
            services.TryAddScoped<ISessionIdResolver, CompositeSessionIdResolver>();

            services.TryAddScoped<ITenantResolver, UAuthTenantResolver>();

            services.TryAddScoped(typeof(IUAuthFlowService<>), typeof(UAuthFlowService<>));
            services.TryAddScoped(typeof(IRefreshFlowService), typeof(DefaultRefreshFlowService));
            services.TryAddScoped(typeof(IUAuthSessionManager), typeof(UAuthSessionManager));

            services.TryAddSingleton<IClock, SystemClock>();

            // TODO: Allow custom cookie manager via options
            //services.AddSingleton<IUAuthCookieManager, UAuthSessionCookieManager>();
            //if (options.CustomCookieManagerType is not null)
            //{
            //    services.AddSingleton(typeof(IUAuthSessionCookieManager), options.CustomCookieManagerType);
            //}
            //else
            //{
            //    services.AddSingleton<IUAuthSessionCookieManager, UAuthSessionCookieManager>();
            //}

            // -----------------------------
            // SESSION / TOKEN ISSUERS
            // -----------------------------
            services.TryAddScoped(typeof(ISessionIssuer), typeof(UAuthSessionIssuer));
            services.TryAddScoped(typeof(ITokenIssuer), typeof(UAuthTokenIssuer));

            services.TryAddScoped(typeof(IUserAccessor<UserKey>), typeof(UAuthUserAccessor<UserKey>));
            services.TryAddScoped<IUserAccessor, UserAccessorBridge>();

            //services.TryAddScoped(typeof(IUserAuthenticator<>), typeof(DefaultUserAuthenticator<>));
            services.TryAddScoped(typeof(ISessionOrchestrator), typeof(UAuthSessionOrchestrator));
            services.TryAddScoped(typeof(ILoginOrchestrator<>), typeof(DefaultLoginOrchestrator<>));
            services.TryAddScoped<IAccessOrchestrator, UAuthAccessOrchestrator>();
            services.TryAddScoped<ILoginAuthority, DefaultLoginAuthority>();
            services.TryAddScoped<IAuthAuthority, DefaultAuthAuthority>();
            services.TryAddScoped<IAccessAuthority, DefaultAccessAuthority>();
            services.TryAddScoped(typeof(ISessionQueryService), typeof(UAuthSessionQueryService));
            services.TryAddScoped(typeof(IRefreshTokenResolver), typeof(DefaultRefreshTokenResolver));
            services.TryAddScoped(typeof(ISessionTouchService), typeof(DefaultSessionTouchService));
            services.TryAddScoped<IDeviceResolver, DefaultDeviceResolver>();
            services.TryAddScoped<ICredentialResponseWriter, DefaultCredentialResponseWriter>();
            services.TryAddScoped<IFlowCredentialResolver, DefaultFlowCredentialResolver>();
            services.TryAddScoped<ITransportCredentialResolver, DefaultTransportCredentialResolver>();
            services.TryAddScoped<IPrimaryCredentialResolver, DefaultPrimaryCredentialResolver>();
            services.TryAddScoped<IRefreshResponseWriter, DefaultRefreshResponseWriter>();
            services.TryAddScoped<ISessionContextAccessor, DefaultSessionContextAccessor>();
            services.TryAddScoped<AuthRedirectResolver>();

            services.TryAddSingleton<AuthResponseOptionsModeTemplateResolver>();
            services.TryAddSingleton<ClientProfileAuthResponseAdapter>();
            services.TryAddSingleton<IAuthResponseResolver, DefaultAuthResponseResolver>();
            services.TryAddSingleton<IClientProfileReader, DefaultClientProfileReader>();
            services.TryAddSingleton<IEffectiveAuthModeResolver, DefaultEffectiveAuthModeResolver>();
            services.TryAddScoped<IAuthFlowContextFactory, DefaultAuthFlowContextFactory>();
            services.TryAddSingleton<IPrimaryTokenResolver, DefaultPrimaryTokenResolver>();
            services.TryAddScoped<IEffectiveServerOptionsProvider,DefaultEffectiveServerOptionsProvider>();

            services.TryAddScoped(typeof(IRefreshTokenValidator), typeof(DefaultRefreshTokenValidator));
            services.TryAddScoped<IPkceAuthorizationValidator, PkceAuthorizationValidator>();
            services.TryAddScoped(typeof(IRefreshTokenRotationService), typeof(RefreshTokenRotationService));

            services.TryAddSingleton<IUAuthCookiePolicyBuilder, DefaultUAuthCookiePolicyBuilder>();
            services.TryAddSingleton<IUAuthHeaderPolicyBuilder, DefaultUAuthHeaderPolicyBuilder>();
            services.TryAddSingleton<IUAuthBodyPolicyBuilder, DefaultUAuthBodyPolicyBuilder>();
            services.TryAddScoped<IUAuthCookieManager, DefaultUAuthCookieManager>();

            services.TryAddScoped<IRefreshResponsePolicy, DefaultRefreshResponsePolicy>();

            services.TryAddSingleton<IAuthStore, InMemoryAuthStore>();
            services.TryAddScoped<IHubFlowReader, DefaultHubFlowReader>();
            services.TryAddScoped<IHubCredentialResolver, DefaultHubCredentialResolver>();
            services.TryAddScoped<IDeviceContextFactory, DefaultDeviceContextFactory>();
            services.TryAddScoped<ICurrentUser, HttpContextCurrentUser>();
            services.TryAddScoped<IAuthContextFactory, DefaultAuthContextFactory>();

            services.TryAddScoped<IHubCapabilities, HubCapabilities>();

            // -----------------------------
            // ENDPOINTS
            // -----------------------------
            services.AddHttpContextAccessor();

            services.TryAddScoped<IAuthFlow, DefaultAuthFlow>();
            services.TryAddScoped<IAuthFlowContextAccessor, DefaultAuthFlowContextAccessor>();
            services.TryAddScoped<IAuthFlowContextFactory, DefaultAuthFlowContextFactory>();
            services.TryAddScoped<IAccessContextFactory, DefaultAccessContextFactory>();

            services.TryAddScoped<AuthFlowEndpointFilter>();


            services.TryAddSingleton<IAuthEndpointRegistrar, UAuthEndpointRegistrar>();
            
            // Endpoint handlers
            services.TryAddScoped<DefaultLoginEndpointHandler<UserKey>>();
            services.TryAddScoped<ILoginEndpointHandler, LoginEndpointHandlerBridge>();

            services.TryAddScoped<DefaultValidateEndpointHandler>();
            services.TryAddScoped<IValidateEndpointHandler, ValidateEndpointHandlerBridge>();

            services.TryAddScoped<DefaultLogoutEndpointHandler<UserKey>>();
            services.TryAddScoped<ILogoutEndpointHandler, LogoutEndpointHandlerBridge>();

            services.TryAddScoped<DefaultRefreshEndpointHandler>();
            services.TryAddScoped<IRefreshEndpointHandler, RefreshEndpointHandlerBridge>();

            services.TryAddScoped<DefaultPkceEndpointHandler<UserKey>>();
            services.TryAddScoped<IPkceEndpointHandler, PkceEndpointHandlerBridge>();
            //services.TryAddScoped<IReauthEndpointHandler, ReauthEndpointHandler>();
            //services.TryAddScoped<ITokenEndpointHandler, TokenEndpointHandler>();
            //services.TryAddScoped<IUserInfoEndpointHandler, UserInfoEndpointHandler>();

            return services;
        }

        //internal static IServiceCollection AddUltimateAuthPolicies(this IServiceCollection services, Action<AccessPolicyRegistry>? configure = null)
        //{
        //    if (services.Any(d => d.ServiceType == typeof(AccessPolicyRegistry)))
        //        throw new InvalidOperationException("UltimateAuth policies already registered.");

        //    var registry = new AccessPolicyRegistry();

        //    DefaultPolicySet.Register(registry);
        //    configure?.Invoke(registry);
        //    services.AddSingleton(registry);
        //    services.AddSingleton<IAccessPolicyProvider>(sp =>
        //    {
        //        var compiled = registry.Build();
        //        return new DefaultAccessPolicyProvider(compiled, sp);
        //    });

        //    services.TryAddScoped<IAccessAuthority>(sp =>
        //    {
        //        var invariants = sp.GetServices<IAccessInvariant>();
        //        var globalPolicies = sp.GetServices<IAccessPolicy>();

        //        return new DefaultAccessAuthority(invariants, globalPolicies);
        //    });

        //    services.TryAddScoped<IAccessOrchestrator, UAuthAccessOrchestrator>();


        //    return services;
        //}

        internal static IServiceCollection AddUltimateAuthPolicies(this IServiceCollection services, Action<AccessPolicyRegistry>? configure = null)
        {
            if (services.Any(d => d.ServiceType == typeof(AccessPolicyRegistry)))
                throw new InvalidOperationException("UltimateAuth policies already registered.");

            var registry = new AccessPolicyRegistry();

            DefaultPolicySet.Register(registry);
            configure?.Invoke(registry);

            // 1. Registry (global, mutable until Build)
            services.AddSingleton(registry);

            // 2. Compiled policy set (immutable, singleton)
            services.AddSingleton(sp =>
            {
                var r = sp.GetRequiredService<AccessPolicyRegistry>();
                return r.Build();
            });

            // 3. Policy provider MUST be scoped
            services.AddScoped<IAccessPolicyProvider, DefaultAccessPolicyProvider>();

            // 4. Authority (scoped, correct)
            services.TryAddScoped<IAccessAuthority>(sp =>
            {
                var invariants = sp.GetServices<IAccessInvariant>();
                var globalPolicies = sp.GetServices<IAccessPolicy>();
                return new DefaultAccessAuthority(invariants, globalPolicies);
            });

            // 5. Orchestrator (scoped)
            services.TryAddScoped<IAccessOrchestrator, UAuthAccessOrchestrator>();

            return services;
        }

        // =========================
        // USERS (FRAMEWORK-REQUIRED)
        // =========================
        internal static IServiceCollection AddUsersInternal(IServiceCollection services)
        {
            // Core user abstractions
            services.TryAddScoped(typeof(IUserAccessor<UserKey>), typeof(UAuthUserAccessor<UserKey>));
            services.TryAddScoped<IUserAccessor, UserAccessorBridge>();

            // Security state
            //services.TryAddScoped(typeof(IUserSecurityEvents<>), typeof(DefaultUserSecurityEvents<>));

            // TODO: Move this into AddAuthorizaionInternal method
            services.TryAddScoped(typeof(IUserClaimsProvider), typeof(DefaultAuthorizationClaimsProvider));

            return services;
        }

        // =========================
        // CREDENTIALS (FRAMEWORK-REQUIRED)
        // =========================
        internal static IServiceCollection AddCredentialsInternal(IServiceCollection services)
        {
            services.TryAddScoped<ICredentialValidator, DefaultCredentialValidator>();
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
            services.TryAddSingleton<IUAuthCookieManager, DefaultUAuthCookieManager>();

            return services;
        }

    }
}
