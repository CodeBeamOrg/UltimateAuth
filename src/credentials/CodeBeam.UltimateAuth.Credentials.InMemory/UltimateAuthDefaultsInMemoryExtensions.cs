using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Credentials.InMemory.Extensions
{
    public static class UltimateAuthCredentialsInMemoryExtensions
    {
        public static IServiceCollection AddUltimateAuthCredentialsInMemory(this IServiceCollection services)
        {
            services.TryAddScoped(typeof(InMemoryCredentialStore<>));
            services.TryAddScoped(typeof(ICredentialStore<>), typeof(InMemoryCredentialStore<>));
            services.TryAddScoped(typeof(ICredentialSecretStore<>), typeof(InMemoryCredentialStore<>));

            return services;
        }
    }
}
