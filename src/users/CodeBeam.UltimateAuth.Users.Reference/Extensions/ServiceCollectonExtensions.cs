using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Credentials.Policies;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace CodeBeam.UltimateAuth.Users.Reference.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthUsersReference(this IServiceCollection services)
    {
        services.PostConfigure<UsersReferenceMarker>(_ =>
        {
            // Marker only – runtime validation happens via DI resolution
        });

        services.TryAddScoped<IUserRuntimeStateProvider, UserRuntimeStateProvider>();
        services.TryAddScoped<IUserApplicationService, UserApplicationService>();
        services.TryAddScoped<IUserIdentifierAvailabilityService, UserIdentifierAvailabilityService>();
        services.TryAddScoped<IUserEndpointHandler, UserEndpointHandler>();
        services.TryAddScoped<IPrimaryUserIdentifierProvider, PrimaryUserIdentifierProvider>();
        services.TryAddScoped<IUserProfileSnapshotProvider, UserProfileSnapshotProvider>();
        services.TryAddScoped<IUserLifecycleSnapshotProvider, UserLifecycleSnapshotProvider>();
        services.AddScoped<ILoginIdentifierResolver, LoginIdentifierResolver>();
        services.AddScoped<IPasswordPolicyContextProvider, UserPasswordPolicyContextProvider>();

        services.TryAddScoped<IUserIdentifierVerifier, NotConfiguredUserIdentifierVerifier>();

        return services;
    }

    private sealed class UsersReferenceMarker;
}
