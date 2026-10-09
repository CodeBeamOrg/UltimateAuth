using CodeBeam.UltimateAuth.InMemory.Extensions;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Users.InMemory.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthUsersInMemory(this IServiceCollection services)
    {
        services.AddUltimateAuthInMemoryInfrastructure();

        services.AddSingleton<IUserLifecycleStoreFactory, InMemoryUserLifecycleStoreFactory>();
        services.AddSingleton<IUserIdentifierStoreFactory, InMemoryUserIdentifierStoreFactory>();
        services.AddSingleton<IUserProfileStoreFactory, InMemoryUserProfileStoreFactory>();
        services.AddSingleton<IUserSummaryQueryStoreFactory, InMemoryUserSummaryQueryStoreFactory>();

        return services;
    }
}
