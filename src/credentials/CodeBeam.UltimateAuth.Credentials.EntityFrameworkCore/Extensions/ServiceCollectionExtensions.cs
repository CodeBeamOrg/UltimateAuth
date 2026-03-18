using CodeBeam.UltimateAuth.Credentials.Reference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthCredentialsEntityFrameworkCore(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContext<UAuthCredentialDbContext>(configureDb);
        services.AddScoped<IPasswordCredentialStoreFactory, EfCorePasswordCredentialStoreFactory>();
        return services;
    }
}
