using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Users.InMemory.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthUsersInMemory(this IServiceCollection services)
    {
        services.TryAddSingleton<IUserLifecycleStore, InMemoryUserLifecycleStore>();
        services.TryAddSingleton<IUserIdentifierStore, InMemoryUserIdentifierStore>();
        services.TryAddSingleton<IUserProfileStore, InMemoryUserProfileStore>();
        services.TryAddSingleton(typeof(InMemoryUserSecurityStore<>));
        services.TryAddScoped(typeof(IUserSecurityStateProvider<>), typeof(InMemoryUserSecurityStateProvider<>));
        services.TryAddScoped(typeof(IUserSecurityStateWriter<>), typeof(InMemoryUserSecurityStateWriter<>));
        services.TryAddSingleton<IInMemoryUserIdProvider<UserKey>, InMemoryUserIdProvider>();

        // Seed never try add
        services.AddSingleton<ISeedContributor, InMemoryUserSeedContributor>();

        return services;
    }
}
