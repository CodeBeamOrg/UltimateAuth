using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Issuers;
using CodeBeam.UltimateAuth.Server.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Extensions
{
    public static class UAuthServerServiceCollectionExtensions
    {
        public static IServiceCollection AddUltimateAuthServer(
            this IServiceCollection services)
        {
            services.AddUltimateAuth(); // Core
            return services.AddUltimateAuthServerInternal();
        }

        public static IServiceCollection AddUltimateAuthServer(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddUltimateAuth(configuration); // Core
            services.Configure<UAuthServerOptions>(
                configuration.GetSection("UltimateAuth:Server"));

            services.Configure<UAuthSessionResolutionOptions>(
                configuration.GetSection("UltimateAuth:SessionResolution"));

            return services.AddUltimateAuthServerInternal();
        }

        public static IServiceCollection AddUltimateAuthServer(
            this IServiceCollection services,
            Action<UAuthServerOptions> configure)
        {
            services.AddUltimateAuth(); // Core
            services.Configure(configure);

            return services.AddUltimateAuthServerInternal();
        }

        private static IServiceCollection AddUltimateAuthServerInternal(
    this IServiceCollection services)
        {
            // -----------------------------
            // OPTIONS VALIDATION
            // -----------------------------
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<
                    IValidateOptions<UAuthServerOptions>,
                    UAuthServerOptionsValidator>());

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

            services.TryAddScoped<ITenantResolver, UAuthTenantResolver>();

            // -----------------------------
            // SESSION / TOKEN ISSUERS
            // -----------------------------
            services.TryAddScoped(
                typeof(UAuthSessionIssuer<>),
                typeof(UAuthSessionIssuer<>));

            services.TryAddScoped<UAuthTokenIssuer>();

            // -----------------------------
            // ENDPOINTS
            // -----------------------------
            services.TryAddSingleton<IAuthEndpointRegistrar, UAuthEndpointRegistrar>();

            return services;
        }

    }
}
