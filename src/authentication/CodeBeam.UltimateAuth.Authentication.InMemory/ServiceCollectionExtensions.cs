using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.InMemory.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Authentication.InMemory.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthAuthenticationInMemory(this IServiceCollection services)
    {
        services.AddUltimateAuthInMemoryInfrastructure();

        services.AddSingleton<IAuthenticationSecurityStateStoreFactory, InMemoryAuthenticationSecurityStateStoreFactory>();
        return services;
    }
}
