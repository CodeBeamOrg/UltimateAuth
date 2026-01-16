using CodeBeam.UltimateAuth.Server.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Users.InMemory.Extensions
{
    public static class UltimateAuthUsersInMemoryExtensions
    {
        public static IServiceCollection AddUltimateAuthUsersInMemory(this IServiceCollection services)
        {
            services.TryAddScoped(typeof(InMemoryUserStore<>));
            services.Replace(ServiceDescriptor.Scoped(typeof(IUserStore<>), typeof(InMemoryUserStore<>)));
            services.Replace(ServiceDescriptor.Scoped(typeof(IUserSecurityStateProvider<>), typeof(InMemoryUserSecurityStateProvider<>)));
            services.Replace(ServiceDescriptor.Scoped(typeof(IUserClaimsProvider<>), typeof(InMemoryUserClaimsProvider<>)));

            return services;
        }
    }
}
