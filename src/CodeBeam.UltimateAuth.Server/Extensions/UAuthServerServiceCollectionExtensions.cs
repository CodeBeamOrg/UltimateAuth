using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Cookies;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Issuers;
using CodeBeam.UltimateAuth.Server.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
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
            services.Configure<UAuthSessionResolutionOptions>(configuration.GetSection("UltimateAuth:SessionResolution"));

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
            services.AddOptions<UAuthServerOptions>()
                .PostConfigure(o =>
                {
                    ConfigureDefaults.ApplyClientProfileDefaults(o);
                    ConfigureDefaults.ApplyModeDefaults(o);
                });

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
            services.AddScoped<IInnerSessionIdResolver, BearerSessionIdResolver>();
            services.AddScoped<IInnerSessionIdResolver, HeaderSessionIdResolver>();
            services.AddScoped<IInnerSessionIdResolver, CookieSessionIdResolver>();
            services.AddScoped<IInnerSessionIdResolver, QuerySessionIdResolver>();

            // Public resolver (tek!)
            services.AddScoped<ISessionIdResolver, CompositeSessionIdResolver>();

            services.TryAddScoped<ITenantResolver, UAuthTenantResolver>();

            services.AddScoped(typeof(IUAuthFlowService<>), typeof(UAuthFlowService<>));
            services.AddScoped(typeof(IUAuthSessionService<>), typeof(UAuthSessionService<>));
            services.AddScoped(typeof(IUAuthUserService<>), typeof(UAuthUserService<>));
            services.AddScoped(typeof(IUAuthTokenService<>), typeof(UAuthTokenService<>));

            services.AddSingleton<IClock, SystemClock>();

            // TODO: Allow custom cookie manager via options
            services.AddSingleton<IUAuthSessionCookieManager, UAuthSessionCookieManager>();
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
            services.TryAddScoped<ITokenIssuer, UAuthTokenIssuer>();

            services.TryAddScoped(typeof(IUserAccessor<UserId>), typeof(UAuthUserAccessor<UserId>));
            services.TryAddScoped<IUserAccessor, UserAccessorBridge>();

            services.TryAddScoped(typeof(IUserAuthenticator<>), typeof(DefaultUserAuthenticator<>));
            services.TryAddScoped(typeof(ISessionOrchestrator<>), typeof(UAuthSessionOrchestrator<>));
            services.TryAddScoped<IAuthAuthority, DefaultAuthAuthority>();
            services.TryAddScoped(typeof(ISessionQueryService<>), typeof(UAuthSessionQueryService<>));
            services.TryAddScoped(typeof(IRefreshTokenResolver<>), typeof(UAuthRefreshTokenResolver<>));
            services.TryAddScoped<IDeviceResolver, DefaultDeviceResolver>();

            // -----------------------------
            // ENDPOINTS
            // -----------------------------
            services.TryAddSingleton<IAuthEndpointRegistrar, UAuthEndpointRegistrar>();
            // Endpoint handlers
            //services.TryAddScoped(typeof(ILoginEndpointHandler), typeof(DefaultLoginEndpointHandler<>));
            services.AddScoped<DefaultLoginEndpointHandler<UserId>>();
            services.AddScoped<ILoginEndpointHandler, LoginEndpointHandlerBridge>();
            //services.TryAddScoped<ILogoutEndpointHandler, LogoutEndpointHandler>();
            //services.TryAddScoped<ISessionRefreshEndpointHandler, SessionRefreshEndpointHandler>();
            //services.TryAddScoped<IReauthEndpointHandler, ReauthEndpointHandler>();
            //services.TryAddScoped<IPkceEndpointHandler, PkceEndpointHandler>();
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
            services.TryAddScoped<ITokenIssuer, UAuthTokenIssuer>();

            // User service
            services.TryAddScoped(typeof(IUAuthUserService<>), typeof(UAuthUserService<>));

            // Endpoints
            services.TryAddSingleton<IAuthEndpointRegistrar, UAuthEndpointRegistrar>();

            // Cookie management (default)
            services.TryAddSingleton<IUAuthSessionCookieManager, UAuthSessionCookieManager>();

            return services;
        }

    }
}
