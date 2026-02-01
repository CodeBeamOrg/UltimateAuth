using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthEntityFrameworkCoreSessions<TUserId>(this IServiceCollection services,Action<DbContextOptionsBuilder> configureDb)where TUserId : notnull
    {
        services.AddDbContext<UltimateAuthSessionDbContext>(configureDb);
        services.AddScoped<EfCoreSessionStoreKernel>();

        return services;
    }
}
