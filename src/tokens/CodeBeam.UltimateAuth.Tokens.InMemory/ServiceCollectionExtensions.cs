using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tokens.InMemory;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthInMemoryTokens(this IServiceCollection services)
    {
        services.AddSingleton<ITokenStoreFactory, InMemoryTokenStoreFactory>();
        services.AddScoped(typeof(ITokenStore<>), typeof(InMemoryTokenStore<>));

        return services;
    }
}
