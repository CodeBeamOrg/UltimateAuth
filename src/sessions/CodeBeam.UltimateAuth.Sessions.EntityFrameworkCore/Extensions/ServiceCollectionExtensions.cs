using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthSessionsEntityFrameworkCore(this IServiceCollection services,Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContext<UltimateAuthSessionDbContext>(configureDb);
        services.AddScoped<ISessionStoreFactory, EfCoreSessionStoreFactory>();

        return services;
    }
}
