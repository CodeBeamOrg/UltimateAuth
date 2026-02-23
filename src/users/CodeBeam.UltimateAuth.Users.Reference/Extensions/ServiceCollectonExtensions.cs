using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Core.Abstractions;
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
        services.TryAddScoped<IUserEndpointHandler, UserEndpointHandler>();
        services.TryAddScoped<IPrimaryUserIdentifierProvider, PrimaryUserIdentifierProvider>();
        services.TryAddScoped<IUserProfileSnapshotProvider, UserProfileSnapshotProvider>();

        return services;
    }

    private sealed class UsersReferenceMarker;
}
