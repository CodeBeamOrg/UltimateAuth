using CodeBeam.UltimateAuth.Credentials.Reference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthEntityFrameworkCoreCredentials(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContextPool<UAuthCredentialDbContext>(configureDb);
        services.AddScoped<IPasswordCredentialStore, EfCorePasswordCredentialStore>();
        return services;
    }
}
