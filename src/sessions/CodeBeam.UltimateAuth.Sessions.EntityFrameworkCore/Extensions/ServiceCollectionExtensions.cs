using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthSessionsEntityFrameworkCore<TDbContext>(this IServiceCollection services, Action<DbContextOptionsBuilder>? configureDb = null) where TDbContext : DbContext
    {
        if (configureDb != null)
        {
            services.AddDbContext<TDbContext>(configureDb);
        }
        
        services.AddScoped<ISessionStoreFactory, EfCoreSessionStoreFactory<TDbContext>>();

        return services;
    }
}
