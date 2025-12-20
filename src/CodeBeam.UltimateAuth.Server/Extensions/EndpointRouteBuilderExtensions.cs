using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Extensions
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointRouteBuilder MapUAuthEndpoints(this IEndpointRouteBuilder endpoints)
        {
            using var scope = endpoints.ServiceProvider.CreateScope();

            var registrar = scope.ServiceProvider
                .GetRequiredService<IAuthEndpointRegistrar>();

            var options = scope.ServiceProvider
                .GetRequiredService<IOptions<UAuthServerOptions>>()
                .Value;

            // Root group ("/")
            var rootGroup = endpoints.MapGroup("");

            registrar.MapEndpoints(rootGroup, options);

            return endpoints;
        }
    }
}
