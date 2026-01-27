using CodeBeam.UltimateAuth.Server.Endpoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeBeam.UltimateAuth.Authorization.Reference.Extensions
{
    public static class AuthorizationReferenceExtensions
    {
        public static IServiceCollection AddUltimateAuthAuthorizationReference(this IServiceCollection services)
        {
            services.TryAddScoped<IAuthorizationService, DefaultAuthorizationService>();
            services.TryAddScoped<IRolePermissionResolver, DefaultRolePermissionResolver>();
            services.TryAddScoped<IUserRoleService, DefaultUserRoleService>();
            services.TryAddScoped<IUserPermissionStore, DefaultUserPermissionStore>();
            services.TryAddScoped<IAuthorizationEndpointHandler, DefaultAuthorizationEndpointHandler>();

            return services;
        }
    }

}
