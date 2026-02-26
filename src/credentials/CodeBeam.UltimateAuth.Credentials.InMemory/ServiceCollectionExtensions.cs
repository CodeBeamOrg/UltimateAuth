using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Credentials.InMemory.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUltimateAuthCredentialsInMemory(this IServiceCollection services)
        {
            services.TryAddScoped<InMemoryCredentialStore>();
            services.TryAddSingleton<ICredentialStore, InMemoryCredentialStore>();

            // Never try add seed
            services.AddSingleton<ISeedContributor, InMemoryCredentialSeedContributor>();

            return services;
        }
    }
}
