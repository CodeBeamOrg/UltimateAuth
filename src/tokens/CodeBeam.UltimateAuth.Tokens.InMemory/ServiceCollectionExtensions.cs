using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tokens.InMemory.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthTokensInMemory(this IServiceCollection services)
    {
        services.AddSingleton<IRefreshTokenStoreFactory, InMemoryRefreshTokenStoreFactory>();
        return services;
    }
}
