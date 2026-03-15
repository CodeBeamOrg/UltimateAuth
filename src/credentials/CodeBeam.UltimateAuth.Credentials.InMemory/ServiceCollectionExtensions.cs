using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Credentials.Reference;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Credentials.InMemory.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUltimateAuthCredentialsInMemory(this IServiceCollection services)
        {
            services.TryAddScoped<InMemoryPasswordCredentialStore>();
            services.TryAddSingleton<IPasswordCredentialStore, InMemoryPasswordCredentialStore>();

            // Never try add seed
            services.AddSingleton<ISeedContributor, InMemoryCredentialSeedContributor>();

            return services;
        }
    }
}
