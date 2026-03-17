using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthTokensEntityFrameworkCore(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContextPool<UltimateAuthTokenDbContext>(configureDb);
        services.AddScoped<IRefreshTokenStoreFactory, EfCoreRefreshTokenStoreFactory>();
        return services;
    }
}
