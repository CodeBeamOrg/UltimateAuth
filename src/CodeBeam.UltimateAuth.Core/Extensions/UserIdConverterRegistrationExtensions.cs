using Microsoft.Extensions.DependencyInjection;
using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Core.Extensions
{
    /// <summary>
    /// Provides extension methods for registering custom <see cref="IUserIdConverter{TUserId}"/>
    /// implementations into the dependency injection container.
    /// 
    /// UltimateAuth internally relies on user ID normalization for:
    /// - session store lookups
    /// - token generation and validation
    /// - logging and diagnostics
    /// - multi-tenant user routing
    /// 
    /// By default, a simple "UAuthUserIdConverter{TUserId}" is used, but
    /// applications may override this with stronger or domain-specific converters
    /// (e.g., ULIDs, Snowflakes, encrypted identifiers, composite keys).
    /// </summary>
    public static class UserIdConverterRegistrationExtensions
    {
        /// <summary>
        /// Registers a custom <see cref="IUserIdConverter{TUserId}"/> implementation.
        /// 
        /// Use this overload when you want to supply your own converter type.
        /// Ideal for stateless converters that simply translate user IDs to/from
        /// string or byte representations (database keys, token subjects, etc.).
        /// 
        /// The converter is registered as a singleton because:
        /// - conversion is pure and stateless,
        /// - high-performance lookup is required,
        /// - converters are reused across multiple services (tokens, sessions, stores).
        /// </summary>
        /// <typeparam name="TUserId">The application's user ID type.</typeparam>
        /// <typeparam name="TConverter">The custom converter implementation.</typeparam>
        public static IServiceCollection AddUltimateAuthUserIdConverter<TUserId, TConverter>(
            this IServiceCollection services)
            where TConverter : class, IUserIdConverter<TUserId>
        {
            services.AddSingleton<IUserIdConverter<TUserId>, TConverter>();
            return services;
        }

#pragma warning disable CS1573
        /// <summary>
        /// Registers a specific instance of <see cref="IUserIdConverter{TUserId}"/>.
        /// 
        /// Use this overload when:
        /// - the converter requires configuration or external initialization,
        /// - the converter contains state (e.g., encryption keys, salt pools),
        /// - multiple converters need DI-managed lifetime control.
        /// </summary>
        /// <typeparam name="TUserId">The application's user ID type.</typeparam>
        /// <param name="instance">The converter instance to register.</param>
        public static IServiceCollection AddUltimateAuthUserIdConverter<TUserId>(
            this IServiceCollection services,
            IUserIdConverter<TUserId> instance)
        {
            services.AddSingleton(instance);
            return services;
        }
    }

}
