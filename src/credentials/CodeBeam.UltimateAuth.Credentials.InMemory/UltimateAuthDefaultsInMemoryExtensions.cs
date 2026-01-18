using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.InMemory.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CodeBeam.UltimateAuth.Core.Infrastructure;

namespace CodeBeam.UltimateAuth.Credentials.InMemory.Extensions
{
    public static class UltimateAuthCredentialsInMemoryExtensions
    {
        public static IServiceCollection AddUltimateAuthCredentialsInMemory(this IServiceCollection services)
        {
            services.TryAddScoped(typeof(InMemoryCredentialStore<>));
            services.Replace(ServiceDescriptor.Scoped(typeof(ICredentialStore<>), typeof(InMemoryCredentialStore<>)));
            services.Replace(ServiceDescriptor.Scoped(typeof(ICredentialSecretStore<>), typeof(InMemoryCredentialStore<>)));
            services.Replace(ServiceDescriptor.Scoped(typeof(ICredentialSecurityVersionStore<>), typeof(InMemoryCredentialStore<>)));
            services.Replace(ServiceDescriptor.Scoped<ICredentialValidator, InMemoryPasswordCredentialValidator>());

            return services;
        }
    }
}
