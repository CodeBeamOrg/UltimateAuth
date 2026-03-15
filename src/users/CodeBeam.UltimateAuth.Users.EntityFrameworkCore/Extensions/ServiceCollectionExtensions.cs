using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthEntityFrameworkCoreUsers(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContextPool<UAuthUserDbContext>(configureDb);
        services.AddScoped<IUserLifecycleStore, EfCoreUserLifecycleStore>();
        services.AddScoped<IUserIdentifierStore, EfCoreUserIdentifierStore>();
        services.AddScoped<IUserProfileStore, EfCoreUserProfileStore>();
        return services;
    }
}
