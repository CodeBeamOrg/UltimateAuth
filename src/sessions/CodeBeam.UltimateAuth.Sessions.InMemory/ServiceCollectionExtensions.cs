using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Sessions.InMemory
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUltimateAuthInMemorySessions(this IServiceCollection services)
        {
            services.AddSingleton<ISessionStoreKernelFactory, InMemorySessionStoreFactory>();
            // TODO: Discuss it to be singleton or scoped
            services.AddScoped<ISessionStore, InMemorySessionStore>();
            return services;
        }
    }
}
