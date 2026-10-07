using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.InMemory.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthInMemoryInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton<InMemoryAtomicCoordinator>();
        services.TryAddSingleton<InMemoryAtomicContextAccessor>();
        services.TryAddScoped<IUAuthAtomicExecutor, InMemoryUAuthAtomicExecutor>();

        return services;
    }
}
