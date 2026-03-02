using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Authentication.InMemory;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthInMemoryAuthenticationSecurity(this IServiceCollection services)
    {
        services.AddSingleton<IAuthenticationSecurityStateStore, InMemoryAuthenticationSecurityStateStore>();

        return services;
    }
}
