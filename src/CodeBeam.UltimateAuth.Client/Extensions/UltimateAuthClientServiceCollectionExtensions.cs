using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Extensions
{
    /// <summary>
    /// Provides extension methods for registering UltimateAuth client services.
    ///
    /// This layer is responsible for:
    /// - Client-side authentication actions (login, logout, refresh, reauth)
    /// - Browser-based POST infrastructure (JS form submit)
    /// - Endpoint configuration for auth mutations
    ///
    /// This extension can safely be used together with AddUltimateAuthServer()
    /// in Blazor Server applications.
    /// </summary>
    public static class UltimateAuthClientServiceCollectionExtensions
    {
        /// <summary>
        /// Registers UltimateAuth client services using configuration binding
        /// (e.g. appsettings.json).
        /// </summary>
        public static IServiceCollection AddUltimateAuthClient(this IServiceCollection services, IConfiguration configurationSection)
        {
            services.Configure<UAuthClientOptions>(configurationSection);
            return services.AddUltimateAuthClientInternal();
        }

        /// <summary>
        /// Registers UltimateAuth client services using programmatic configuration.
        /// </summary>
        public static IServiceCollection AddUltimateAuthClient(this IServiceCollection services, Action<UAuthClientOptions> configure)
        {
            services.Configure(configure);
            return services.AddUltimateAuthClientInternal();
        }

        /// <summary>
        /// Registers UltimateAuth client services with default (empty) configuration.
        ///
        /// Intended for advanced scenarios where configuration is fully controlled
        /// by the hosting application or overridden later.
        /// </summary>
        public static IServiceCollection AddUltimateAuthClient(this IServiceCollection services)
        {
            services.Configure<UAuthClientOptions>(_ => { });
            return services.AddUltimateAuthClientInternal();
        }

        /// <summary>
        /// Internal shared registration pipeline for UltimateAuth client services.
        ///
        /// This method registers:
        /// - Client infrastructure
        /// - Public client abstractions
        ///
        /// NOTE:
        /// This method does NOT register any server-side services.
        /// </summary>
        private static IServiceCollection AddUltimateAuthClientInternal(
            this IServiceCollection services)
        {
            // Options validation can be added here later if needed
            // services.AddSingleton<IValidateOptions<UAuthClientOptions>, ...>();

            services.AddScoped<IBrowserPostClient, BrowserPostClient>();
            services.AddScoped<IUAuthClient, UAuthClient>();

            return services;
        }
    }
}
