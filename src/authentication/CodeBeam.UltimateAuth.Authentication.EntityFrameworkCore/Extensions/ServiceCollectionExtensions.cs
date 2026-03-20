using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Authentication.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthAuthenticationEntityFrameworkCore(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContext<UAuthAuthenticationDbContext>(configureDb);
        services.AddScoped<IAuthenticationSecurityStateStoreFactory, EfCoreAuthenticationSecurityStateStoreFactory>();
        return services;
    }
}
