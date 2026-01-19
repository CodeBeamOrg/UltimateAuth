using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Authorization.InMemory.Extensions
{
    public static class AuthorizationInMemoryExtensions
    {
        public static IServiceCollection AddUltimateAuthAuthorizationInMemory(this IServiceCollection services)
        {
            services.AddSingleton<IUserRoleStore, InMemoryUserRoleStore>();
            services.AddSingleton<IAuthorizationSeeder, InMemoryAuthorizationSeeder>();

            return services;
        }
    }
}
