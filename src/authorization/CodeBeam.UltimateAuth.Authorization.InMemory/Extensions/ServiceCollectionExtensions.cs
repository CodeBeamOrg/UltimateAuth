using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.InMemory.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Authorization.InMemory.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthAuthorizationInMemory(this IServiceCollection services)
    {
        services.AddUltimateAuthInMemoryInfrastructure();

        services.TryAddSingleton<IRoleStoreFactory, InMemoryRoleStoreFactory>();
        services.TryAddSingleton<IUserRoleStoreFactory, InMemoryUserRoleStoreFactory>();

        return services;
    }
}
