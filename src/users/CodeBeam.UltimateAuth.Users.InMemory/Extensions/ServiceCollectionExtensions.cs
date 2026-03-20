using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.InMemory;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Users.InMemory.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthUsersInMemory(this IServiceCollection services)
    {
        services.AddSingleton<IUserLifecycleStoreFactory, InMemoryUserLifecycleStoreFactory>();
        services.AddSingleton<IUserIdentifierStoreFactory, InMemoryUserIdentifierStoreFactory>();
        services.AddSingleton<IUserProfileStoreFactory, InMemoryUserProfileStoreFactory>();
        services.TryAddSingleton<IInMemoryUserIdProvider<UserKey>, InMemoryUserIdProvider>();

        // Seed never try add
        services.AddSingleton<ISeedContributor, InMemoryUserSeedContributor>();

        return services;
    }
}
