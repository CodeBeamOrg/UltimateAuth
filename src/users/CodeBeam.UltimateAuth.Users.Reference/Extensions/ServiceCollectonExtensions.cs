using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.Users.Reference.Services;
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

        services.TryAddScoped<IUAuthUserProfileService, DefaultUserProfileService>();
        services.TryAddScoped<IUserLifecycleService, DefaultUserLifecycleService>();

        return services;
    }

    private sealed class UsersReferenceMarker;
}
