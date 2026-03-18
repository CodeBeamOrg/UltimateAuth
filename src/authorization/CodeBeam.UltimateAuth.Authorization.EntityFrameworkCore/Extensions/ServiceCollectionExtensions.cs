using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthAuthorizationEntityFrameworkCore(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContext<UAuthAuthorizationDbContext>(configureDb);
        services.AddScoped<IRoleStoreFactory, EfCoreRoleStoreFactory>();
        services.AddScoped<IUserRoleStoreFactory, EfCoreUserRoleStoreFactory>();
        return services;
    }
}
