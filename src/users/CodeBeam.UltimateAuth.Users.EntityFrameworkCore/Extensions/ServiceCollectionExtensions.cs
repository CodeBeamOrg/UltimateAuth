using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthUsersEntityFrameworkCore(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContext<UAuthUserDbContext>(configureDb);
        services.AddScoped<IUserLifecycleStoreFactory, EfCoreUserLifecycleStoreFactory>();
        services.AddScoped<IUserIdentifierStoreFactory, EfCoreUserIdentifierStoreFactory>();
        services.AddScoped<IUserProfileStoreFactory, EfCoreUserProfileStoreFactory>();
        return services;
    }
}
