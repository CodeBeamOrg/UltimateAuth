using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Sessions.InMemory.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthSessionsInMemory(this IServiceCollection services)
    {
        services.AddSingleton<ISessionStoreFactory, InMemorySessionStoreFactory>();
        return services;
    }
}
