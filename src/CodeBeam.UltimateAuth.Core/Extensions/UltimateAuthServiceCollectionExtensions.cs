using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            services.Configure<UAuthOptions>(configurationSection);
            return services.AddUltimateAuthInternal();
        }

        /// <summary>
        /// Registers UltimateAuth services using programmatic configuration.
        /// This is useful when settings are derived dynamically or are not stored
        /// in appsettings.json.
        /// </summary>
        public static IServiceCollection AddUltimateAuth(this IServiceCollection services, Action<UAuthOptions> configure)
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
            services.Configure<UAuthOptions>(_ => { });
            return services.AddUltimateAuthInternal();
        }

        /// <summary>
        /// Internal shared registration pipeline invoked by all AddUltimateAuth overloads.
        /// Registers validators, user ID converters, and placeholder factories.
        /// Core-level invariant validation.
        /// Server layer may add additional validators.
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
            services.AddSingleton<IValidateOptions<UAuthOptions>, UAuthOptionsValidator>();
            services.AddSingleton<IValidateOptions<UAuthSessionOptions>, UAuthSessionOptionsValidator>();
            services.AddSingleton<IValidateOptions<UAuthTokenOptions>, UAuthTokenOptionsValidator>();
            services.AddSingleton<IValidateOptions<UAuthPkceOptions>, UAuthPkceOptionsValidator>();
            services.AddSingleton<IValidateOptions<UAuthMultiTenantOptions>, UAuthMultiTenantOptionsValidator>();

            // Nested options are bound automatically by the options binder.
            // Server layer may override or extend these settings.

            services.AddSingleton<IUserIdConverterResolver, UAuthUserIdConverterResolver>();

            return services;
        }

    }
}
