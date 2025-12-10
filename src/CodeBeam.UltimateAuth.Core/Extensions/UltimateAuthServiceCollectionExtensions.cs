using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Core.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Core.Extensions
{
    // TODO: Check it before stable release
    public static class UltimateAuthServiceCollectionExtensions
    {
        /// <summary>
        /// Add UltimateAuth using configuration binding. (appsettings.json)
        /// </summary>
        public static IServiceCollection AddUltimateAuth(this IServiceCollection services, IConfiguration configurationSection)
        {
            services.Configure<UltimateAuthOptions>(configurationSection);
            return services.AddUltimateAuthInternal();
        }

        /// <summary>
        /// Add UltimateAuth using inline fluent configuration.
        /// </summary>
        public static IServiceCollection AddUltimateAuth(this IServiceCollection services, Action<UltimateAuthOptions> configure)
        {
            services.Configure(configure);
            return services.AddUltimateAuthInternal();
        }

        /// <summary>
        /// Add UltimateAuth with default settings.
        /// Useful for advanced scenarios or manual configuration.
        /// </summary>
        public static IServiceCollection AddUltimateAuth(this IServiceCollection services)
        {
            services.Configure<UltimateAuthOptions>(_ => { });
            return services.AddUltimateAuthInternal();
        }

        /// <summary>
        /// Internal shared registration pipeline.
        /// Called by all 3 public overloads.
        /// </summary>
        private static IServiceCollection AddUltimateAuthInternal(this IServiceCollection services)
        {
            services.AddSingleton<IValidateOptions<UltimateAuthOptions>, UltimateAuthOptionsValidator>();
            services.AddSingleton<IValidateOptions<SessionOptions>, SessionOptionsValidator>();
            services.AddSingleton<IValidateOptions<TokenOptions>, TokenOptionsValidator>();
            services.AddSingleton<IValidateOptions<PkceOptions>, PkceOptionsValidator>();
            services.AddSingleton<IValidateOptions<MultiTenantOptions>, MultiTenantOptionsValidator>();

            // Binding configurations should be in the server project

            services.AddSingleton<IUserIdConverterResolver, UAuthUserIdConverterResolver>();
            services.TryAddSingleton<ISessionStoreFactory, DefaultSessionStoreFactory>();

            return services;
        }
    }
}
