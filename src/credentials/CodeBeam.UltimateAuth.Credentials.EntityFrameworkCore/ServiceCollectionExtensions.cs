using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthEfCoreCredentials<TUser, TUserId>(this IServiceCollection services, Action<CredentialUserMappingOptions<TUser, TUserId>> configure) where TUser : class
    {
        services.Configure(configure);

        return services;
    }
}
