using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Authorization.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthAuthorizationEntityFrameworkCore(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContextPool<UAuthAuthorizationDbContext>(configureDb);
        services.AddScoped<IRoleStore, EfCoreRoleStore>();
        services.AddScoped<IUserRoleStore, EfCoreUserRoleStore>();
        return services;
    }
}
