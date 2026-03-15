using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthEntityFrameworkCoreSessions(this IServiceCollection services,Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContextPool<UltimateAuthSessionDbContext>(configureDb);
        services.AddScoped<ISessionStoreFactory, EfCoreSessionStoreFactory>();

        return services;
    }
}
