using CodeBeam.UltimateAuth.Server.Endpoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Authorization.Reference.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUltimateAuthAuthorizationReference(this IServiceCollection services)
    {
        services.TryAddScoped<IAuthorizationService, AuthorizationService>();
        services.TryAddScoped<IRolePermissionResolver, RolePermissionResolver>();
        services.TryAddScoped<IUserRoleService, UserRoleService>();
        services.TryAddScoped<IUserPermissionStore, UserPermissionStore>();
        services.TryAddScoped<IAuthorizationEndpointHandler, AuthorizationEndpointHandler>();

        return services;
    }
}
