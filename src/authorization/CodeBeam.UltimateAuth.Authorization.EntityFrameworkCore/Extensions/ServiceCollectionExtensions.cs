using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthAuthorizationEntityFrameworkCore<TDbContext>(this IServiceCollection services, Action<DbContextOptionsBuilder>? configureDb = null) where TDbContext : DbContext
    {
        if (configureDb != null)
        {
            services.AddDbContext<TDbContext>(configureDb);
        }
        
        services.AddScoped<IRoleStoreFactory, EfCoreRoleStoreFactory<TDbContext>>();
        services.AddScoped<IUserRoleStoreFactory, EfCoreUserRoleStoreFactory<TDbContext>>();
        return services;
    }
}
