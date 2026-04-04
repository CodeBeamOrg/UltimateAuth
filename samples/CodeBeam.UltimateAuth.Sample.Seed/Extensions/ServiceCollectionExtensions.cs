using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Sample.Seed.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthSampleSeed(this IServiceCollection services)
    {
        services.TryAddSingleton<SeedRunner>();
        services.AddSingleton<IUserIdProvider<UserKey>, UserIdProvider>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISeedContributor, UserSeedContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISeedContributor, CredentialSeedContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISeedContributor, AuthorizationSeedContributor>());

        return services;
    }

    public static IServiceCollection AddScopedUltimateAuthSampleSeed(this IServiceCollection services)
    {
        services.TryAddScoped<SeedRunner>();
        services.AddSingleton<IUserIdProvider<UserKey>, UserIdProvider>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISeedContributor, UserSeedContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISeedContributor, CredentialSeedContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISeedContributor, AuthorizationSeedContributor>());

        return services;
    }
}
