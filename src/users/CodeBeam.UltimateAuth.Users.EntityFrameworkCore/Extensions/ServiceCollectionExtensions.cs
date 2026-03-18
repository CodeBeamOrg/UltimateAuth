using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthUsersEntityFrameworkCore(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContextFactory<UAuthUserDbContext>(configureDb);
        services.AddSingleton<IUserLifecycleStoreFactory, EfCoreUserLifecycleStoreFactory>();
        services.AddSingleton<IUserIdentifierStoreFactory, EfCoreUserIdentifierStoreFactory>();
        services.AddSingleton<IUserProfileStoreFactory, EfCoreUserProfileStoreFactory>();
        return services;
    }
}
