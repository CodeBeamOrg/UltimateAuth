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
    /// <summary>
    /// Provides extension methods for registering UltimateAuth core services into
    /// the application's dependency injection container.
    /// 
    /// These methods configure options, validators, converters, and factories required
    /// for the authentication subsystem. 
    /// 
    /// IMPORTANT:
    /// This extension registers only CORE services — session stores, token factories,
    /// PKCE handlers, and any server-specific logic must be added from the Server package
    /// (e.g., AddUltimateAuthServer()).
    /// </summary>
    public static class UltimateAuthServiceCollectionExtensions
    {
        /// <summary>
        /// Registers UltimateAuth services using configuration binding (e.g., appsettings.json).
        /// 
        /// The provided configuration section must contain valid UltimateAuthOptions and nested
        /// Session, Token, PKCE, and MultiTenant configuration sections. Validation occurs
        /// at application startup via IValidateOptions.
        /// </summary>
        public static IServiceCollection AddUltimateAuth(this IServiceCollection services, IConfiguration configurationSection)
        {
            services.Configure<UltimateAuthOptions>(configurationSection);
            return services.AddUltimateAuthInternal();
        }

        /// <summary>
        /// Registers UltimateAuth services using programmatic configuration.
        /// This is useful when settings are derived dynamically or are not stored
        /// in appsettings.json.
        /// </summary>
        public static IServiceCollection AddUltimateAuth(this IServiceCollection services, Action<UltimateAuthOptions> configure)
        {
            services.Configure(configure);
            return services.AddUltimateAuthInternal();
        }

        /// <summary>
        /// Registers UltimateAuth services using default empty configuration.
        /// Intended for advanced or fully manual scenarios where options will be
        /// configured later or overridden by the server layer.
        /// </summary>
        public static IServiceCollection AddUltimateAuth(this IServiceCollection services)
        {
            services.Configure<UltimateAuthOptions>(_ => { });
            return services.AddUltimateAuthInternal();
        }

        /// <summary>
        /// Internal shared registration pipeline invoked by all AddUltimateAuth overloads.
        /// Registers validators, user ID converters, and placeholder factories.
        /// 
        /// NOTE:
        /// This method does NOT register session stores or server-side services.
        /// A server project must explicitly call:
        /// 
        ///     services.AddUltimateAuthSessionStore'TStore'();
        /// 
        /// to provide a concrete ISessionStore implementation.
        /// </summary>
        private static IServiceCollection AddUltimateAuthInternal(this IServiceCollection services)
        {
            services.AddSingleton<IValidateOptions<UltimateAuthOptions>, UltimateAuthOptionsValidator>();
            services.AddSingleton<IValidateOptions<SessionOptions>, SessionOptionsValidator>();
            services.AddSingleton<IValidateOptions<TokenOptions>, TokenOptionsValidator>();
            services.AddSingleton<IValidateOptions<PkceOptions>, PkceOptionsValidator>();
            services.AddSingleton<IValidateOptions<MultiTenantOptions>, MultiTenantOptionsValidator>();

            // Binding of nested sub-options (Session, Token, etc.) is intentionally not done here.
            // These must be bound at the server level to allow configuration per-environment.

            services.AddSingleton<IUserIdConverterResolver, UAuthUserIdConverterResolver>();

            // Default factory throws until a real session store is registered.
            services.TryAddSingleton<ISessionStoreFactory, DefaultSessionStoreFactory>();

            return services;
        }

    }
}
