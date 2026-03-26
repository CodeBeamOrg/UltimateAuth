using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Authorization.InMemory.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthAuthorizationInMemory(this IServiceCollection services)
    {
        services.TryAddSingleton<IRoleStoreFactory, InMemoryRoleStoreFactory>();
        services.TryAddSingleton<IUserRoleStoreFactory, InMemoryUserRoleStoreFactory>();

        return services;
    }
}
