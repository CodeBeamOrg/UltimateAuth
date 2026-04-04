using CodeBeam.UltimateAuth.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Security.Argon2;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthArgon2(this IServiceCollection services, Action<Argon2Options>? configure = null)
    {
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<Argon2Options>(_ => { });
        }

        services.AddSingleton<IUAuthPasswordHasher, Argon2PasswordHasher>();

        return services;
    }
}
