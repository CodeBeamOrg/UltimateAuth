using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Server.Extensions
{
    public static class ServiceCollectionTenantExtensions
    {
        public static IServiceCollection AddTenantResolvers(
            this IServiceCollection services)
        {
            services.AddScoped<RouteTenantResolver>();
            services.AddScoped<HeaderTenantResolver>();
            services.AddScoped<DomainTenantResolver>();
            services.AddScoped<AutoTenantResolver>();

            services.AddScoped<ITenantResolver>(sp =>
            {
                var options = sp.GetRequiredService<UAuthMultiTenantOptions>();
                return options.RoutingMode switch
                {
                    UAuthTenantRoutingMode.Route => sp.GetRequiredService<RouteTenantResolver>(),
                    UAuthTenantRoutingMode.Header => sp.GetRequiredService<HeaderTenantResolver>(),
                    UAuthTenantRoutingMode.Domain => sp.GetRequiredService<DomainTenantResolver>(),
                    _ => sp.GetRequiredService<AutoTenantResolver>()
                };
            });

            return services;
        }

    }
}
