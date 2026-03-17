using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Authentication.InMemory.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthAuthenticationInMemory(this IServiceCollection services)
    {
        services.AddSingleton<IAuthenticationSecurityStateStore, InMemoryAuthenticationSecurityStateStore>();
        return services;
    }
}
