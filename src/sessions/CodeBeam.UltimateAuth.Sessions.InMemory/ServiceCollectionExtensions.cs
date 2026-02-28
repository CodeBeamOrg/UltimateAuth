using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Sessions.InMemory;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthInMemorySessions(this IServiceCollection services)
    {
        services.AddSingleton<ISessionStoreFactory, InMemorySessionStoreFactory>();
        return services;
    }
}
