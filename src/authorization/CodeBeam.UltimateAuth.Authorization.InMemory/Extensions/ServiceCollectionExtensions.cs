using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Authorization.InMemory.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthAuthorizationInMemory(this IServiceCollection services)
    {
        services.TryAddSingleton<IUserRoleStore, InMemoryUserRoleStore>();

        // Never try add - seeding is enumerated and all contributors are added.
        services.AddSingleton<ISeedContributor, InMemoryAuthorizationSeedContributor>();

        return services;
    }
}
