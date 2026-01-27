using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Users.InMemory.Extensions
{
    public static class UltimateAuthUsersInMemoryExtensions
    {
        public static IServiceCollection AddUltimateAuthUsersInMemory(this IServiceCollection services)
        {
            services.TryAddScoped<IUserStore<UserKey>, InMemoryUserStore>();
            services.TryAddScoped(typeof(IUserSecurityStateProvider<>), typeof(InMemoryUserSecurityStateProvider<>));
            services.TryAddScoped<IUserLifecycleStore, InMemoryUserLifecycleStore>();
            services.TryAddScoped<IUserIdentifierStore, InMemoryUserIdentifierStore>();
            services.TryAddScoped<IUserProfileStore, InMemoryUserProfileStore>();

            services.TryAddSingleton<IInMemoryUserIdProvider<UserKey>, InMemoryUserIdProvider>();

            return services;
        }
    }
}
