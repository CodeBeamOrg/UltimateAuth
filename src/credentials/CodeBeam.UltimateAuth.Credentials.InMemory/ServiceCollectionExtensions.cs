using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Credentials.InMemory
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the in-memory credential store with a default seeded user.
        /// Intended for development, testing, and reference implementations.
        /// </summary>
        public static IServiceCollection AddInMemoryCredentials(this IServiceCollection services)
        {
            services.AddSingleton(sp =>
            {
                var hasher = sp.GetService<IUAuthPasswordHasher>()
                    ?? throw new InvalidOperationException(
                        "IUAuthPasswordHasher is not registered. " +
                        "Call AddUltimateAuthArgon2() or register a custom hasher.");

                return InMemoryCredentialSeeder.CreateDefaultUsers(hasher);
            });

            services.AddSingleton<IUAuthUserStore<UserId>>(sp =>
            {
                var users = sp.GetRequiredService<IReadOnlyCollection<InMemoryCredentialUser>>();
                return new InMemoryUserStore(users);
            });

            return services;
        }
    }
}
