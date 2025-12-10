using Microsoft.Extensions.DependencyInjection;
using CodeBeam.UltimateAuth.Core.Abstractions;

namespace CodeBeam.UltimateAuth.Core.Extensions
{
    public static class UserIdConverterRegistrationExtensions
    {
        /// <summary>
        /// Registers a custom IUserIdConverter{TUserId} implementation.
        /// This lets developers control how user ids are normalized
        /// across UltimateAuth (sessions, stores, tokens, logging).
        /// </summary>
        public static IServiceCollection AddUltimateAuthUserIdConverter<TUserId, TConverter>(this IServiceCollection services)
            where TConverter : class, IUserIdConverter<TUserId>
        {
            // Converter’ı singleton olarak register ediyoruz.
            // Neden? UserId normalization stateful değildir,
            // performans için singleton olması en iyisidir.
            services.AddSingleton<IUserIdConverter<TUserId>, TConverter>();
            return services;
        }

        /// <summary>
        /// Registers a specific converter instance for TUserId.
        /// Useful when converter state is required or initialized externally.
        /// </summary>
        public static IServiceCollection AddUltimateAuthUserIdConverter<TUserId>(this IServiceCollection services, IUserIdConverter<TUserId> instance)
        {
            services.AddSingleton(instance);
            return services;
        }

    }
}
