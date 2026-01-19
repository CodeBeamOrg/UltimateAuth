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
            services.TryAddScoped<InMemoryUserStore>();
            services.Replace(ServiceDescriptor.Scoped<IUserStore<UserKey>, InMemoryUserStore>());
            services.Replace(ServiceDescriptor.Scoped(typeof(IUserSecurityStateProvider<>), typeof(InMemoryUserSecurityStateProvider<>)));
            services.TryAddScoped<IUserLifecycleStore, InMemoryUserLifecycleStore>();
            services.TryAddSingleton<IInMemoryUserIdProvider<UserKey>, InMemoryUserIdProvider>();
            services.TryAddScoped<IUserProfileStore, InMemoryUserProfileStore>();

            return services;
        }
    }
}
