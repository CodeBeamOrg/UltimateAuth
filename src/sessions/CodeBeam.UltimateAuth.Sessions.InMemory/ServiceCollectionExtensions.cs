using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.InMemory.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Sessions.InMemory.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthSessionsInMemory(this IServiceCollection services)
    {
        services.AddUltimateAuthInMemoryInfrastructure();

        services.AddSingleton<ISessionStoreFactory, InMemorySessionStoreFactory>();
        return services;
    }
}
