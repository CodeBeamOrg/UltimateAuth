using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthEntityFrameworkCore<TDbContext>(this IServiceCollection services) where TDbContext : DbContext
    {
        services.TryAddScoped<IUAuthAtomicExecutor, EfCoreUAuthAtomicExecutor<TDbContext>>();

        return services;
    }
}
