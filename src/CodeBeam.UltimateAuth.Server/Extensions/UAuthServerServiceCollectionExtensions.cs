using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstactions;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Cookies;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Infrastructure.Session;
using CodeBeam.UltimateAuth.Server.Issuers;
using CodeBeam.UltimateAuth.Server.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Server.Stores;
using CodeBeam.UltimateAuth.Server.Users;
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
            return services.AddUltimateAuthServerInternal();
        }

        public static IServiceCollection AddUltimateAuthServer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddUltimateAuth(configuration);
            services.Configure<UAuthServerOptions>(configuration.GetSection("UltimateAuth:Server"));

            return services.AddUltimateAuthServerInternal();
        }

        public static IServiceCollection AddUltimateAuthServer(this IServiceCollection services, Action<UAuthServerOptions> configure)
        {
            services.AddUltimateAuth();
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
            services.AddScoped<ISessionIdResolver, CompositeSessionIdResolver>();

            services.TryAddScoped<ITenantResolver, UAuthTenantResolver>();

            services.AddScoped(typeof(IUAuthFlowService<>), typeof(UAuthFlowService<>));
            services.AddScoped(typeof(IRefreshFlowService<>), typeof(DefaultRefreshFlowService<>));
            services.AddScoped(typeof(IUAuthSessionService<>), typeof(UAuthSessionService<>));
            services.AddScoped(typeof(IUAuthUserService<>), typeof(UAuthUserService<>));

            services.AddSingleton<IClock, SystemClock>();

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
            services.TryAddScoped(typeof(ISessionIssuer<>), typeof(UAuthSessionIssuer<>));
            services.TryAddScoped(typeof(ITokenIssuer<>), typeof(UAuthTokenIssuer<>));

            services.TryAddScoped(typeof(IUserAccessor<UserId>), typeof(UAuthUserAccessor<UserId>));
            services.TryAddScoped<IUserAccessor, UserAccessorBridge>();

            services.TryAddScoped(typeof(IUserAuthenticator<>), typeof(DefaultUserAuthenticator<>));
            services.TryAddScoped(typeof(ISessionOrchestrator<>), typeof(UAuthSessionOrchestrator<>));
            services.TryAddScoped<IAuthAuthority, DefaultAuthAuthority>();
            services.TryAddScoped(typeof(ISessionQueryService<>), typeof(UAuthSessionQueryService<>));
            services.TryAddScoped(typeof(IRefreshTokenResolver), typeof(DefaultRefreshTokenResolver));
            services.TryAddScoped(typeof(ISessionTouchService<>), typeof(DefaultSessionTouchService<>));
            services.TryAddScoped<IDeviceResolver, DefaultDeviceResolver>();
            services.TryAddScoped<ICredentialResponseWriter, DefaultCredentialResponseWriter>();
            services.TryAddScoped<IFlowCredentialResolver, DefaultFlowCredentialResolver>();
            services.AddScoped<ITransportCredentialResolver, DefaultTransportCredentialResolver>();
            services.TryAddScoped<IPrimaryCredentialResolver, DefaultPrimaryCredentialResolver>();
            services.TryAddScoped<IRefreshResponseWriter, DefaultRefreshResponseWriter>();
            services.TryAddScoped<ISessionContextAccessor, DefaultSessionContextAccessor>();
            services.AddScoped<AuthRedirectResolver>();

            services.TryAddSingleton<AuthResponseOptionsModeTemplateResolver>();
            services.TryAddSingleton<ClientProfileAuthResponseAdapter>();
            services.TryAddSingleton<IAuthResponseResolver, DefaultAuthResponseResolver>();
            services.TryAddSingleton<IClientProfileReader, DefaultClientProfileReader>();
            services.TryAddSingleton<IEffectiveAuthModeResolver, DefaultEffectiveAuthModeResolver>();
            services.TryAddScoped<IAuthFlowContextFactory, DefaultAuthFlowContextFactory>();
            services.TryAddSingleton<IPrimaryTokenResolver, DefaultPrimaryTokenResolver>();
            services.AddScoped<IEffectiveServerOptionsProvider,DefaultEffectiveServerOptionsProvider>();

            services.AddScoped(typeof(IRefreshTokenValidator<>), typeof(DefaultRefreshTokenValidator<>));
            services.AddScoped<IPkceAuthorizationValidator, PkceAuthorizationValidator>();
            services.AddScoped(typeof(IRefreshTokenRotationService<>), typeof(RefreshTokenRotationService<>));

            services.AddSingleton<IUAuthCookiePolicyBuilder, DefaultUAuthCookiePolicyBuilder>();
            services.AddSingleton<IUAuthHeaderPolicyBuilder, DefaultUAuthHeaderPolicyBuilder>();
            services.AddSingleton<IUAuthBodyPolicyBuilder, DefaultUAuthBodyPolicyBuilder>();
            services.AddScoped<IUAuthCookieManager, DefaultUAuthCookieManager>();
            services.AddSingleton<ITransportCredentialResolver, DefaultTransportCredentialResolver>();

            services.AddScoped<IRefreshResponsePolicy, DefaultRefreshResponsePolicy>();

            services.AddSingleton<IAuthStore, InMemoryAuthStore>();

            services.AddScoped<IHubFlowStateReader, DefaultHubFlowStateReader>();
            services.AddScoped<IUAuthHubContextAccessor, UAuthHubContextAccessor>();
            // Internal usage
            services.AddScoped<UAuthHubContextAccessor>();
            services.AddScoped<IUAuthHubContextInitializer, DefaultUAuthHubContextInitializer>();

            // -----------------------------
            // ENDPOINTS
            // -----------------------------
            services.AddHttpContextAccessor();

            services.AddScoped<IAuthFlow, DefaultAuthFlow>();
            services.AddScoped<IAuthFlowContextAccessor, DefaultAuthFlowContextAccessor>();
            services.AddScoped<IAuthFlowContextFactory, DefaultAuthFlowContextFactory>();

            services.AddScoped<AuthFlowEndpointFilter>();


            services.TryAddSingleton<IAuthEndpointRegistrar, UAuthEndpointRegistrar>();
            
            // Endpoint handlers
            services.AddScoped<DefaultLoginEndpointHandler<UserId>>();
            services.TryAddScoped<ILoginEndpointHandler, LoginEndpointHandlerBridge>();

            services.AddScoped<DefaultValidateEndpointHandler<UserId>>();
            services.TryAddScoped<IValidateEndpointHandler, ValidateEndpointHandlerBridge>();

            services.AddScoped<DefaultLogoutEndpointHandler<UserId>>();
            services.TryAddScoped<ILogoutEndpointHandler, LogoutEndpointHandlerBridge>();

            services.AddScoped<DefaultRefreshEndpointHandler<UserId>>();
            services.TryAddScoped<IRefreshEndpointHandler, RefreshEndpointHandlerBridge>();

            services.AddScoped<DefaultPkceEndpointHandler<UserId>>();
            services.TryAddScoped<IPkceEndpointHandler, PkceEndpointHandlerBridge>();
            //services.TryAddScoped<IReauthEndpointHandler, ReauthEndpointHandler>();
            //services.TryAddScoped<ITokenEndpointHandler, TokenEndpointHandler>();
            //services.TryAddScoped<IUserInfoEndpointHandler, UserInfoEndpointHandler>();


            return services;
        }

        public static IServiceCollection AddUAuthServerInfrastructure(this IServiceCollection services)
        {
            // Flow orchestration
            services.TryAddScoped(typeof(IUAuthFlowService<>), typeof(UAuthFlowService<>));

            // Issuers
            services.TryAddScoped(typeof(ISessionIssuer<>), typeof(UAuthSessionIssuer<>));
            services.TryAddScoped(typeof(ITokenIssuer<>), typeof(UAuthTokenIssuer<>));

            // User service
            services.TryAddScoped(typeof(IUAuthUserService<>), typeof(UAuthUserService<>));

            // Endpoints
            services.TryAddSingleton<IAuthEndpointRegistrar, UAuthEndpointRegistrar>();

            // Cookie management (default)
            services.TryAddSingleton<IUAuthCookieManager, DefaultUAuthCookieManager>();

            return services;
        }

    }
}
