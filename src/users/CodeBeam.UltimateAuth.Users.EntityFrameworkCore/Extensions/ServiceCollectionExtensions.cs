using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthUsersEntityFrameworkCore<TDbContext>(this IServiceCollection services, Action<DbContextOptionsBuilder>? configureDb = null) where TDbContext : DbContext
    {
        if (configureDb is not null)
        {
            services.AddDbContext<TDbContext>(configureDb);
        }

        services.AddScoped<IUserLifecycleStoreFactory, EfCoreUserLifecycleStoreFactory<TDbContext>>();
        services.AddScoped<IUserIdentifierStoreFactory, EfCoreUserIdentifierStoreFactory<TDbContext>>();
        services.AddScoped<IUserProfileStoreFactory, EfCoreUserProfileStoreFactory<TDbContext>>();
        return services;
    }
}
