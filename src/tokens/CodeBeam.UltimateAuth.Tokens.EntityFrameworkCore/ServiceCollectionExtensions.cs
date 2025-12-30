using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthEntityFrameworkCoreTokens(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContext<UltimateAuthTokenDbContext>(configureDb);
        services.AddScoped<EfCoreTokenStoreKernel>();
        services.AddScoped(typeof(ITokenStore<>), typeof(EfCoreTokenStore<>));

        return services;
    }
}
