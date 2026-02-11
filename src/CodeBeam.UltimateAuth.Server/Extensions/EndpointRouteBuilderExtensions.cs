using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapUAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var registrar = endpoints.ServiceProvider.GetRequiredService<IAuthEndpointRegistrar>();
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<UAuthServerOptions>>().Value;
        var rootGroup = endpoints.MapGroup("");
        registrar.MapEndpoints(rootGroup, options);

        if (endpoints is WebApplication app)
        {
            options.OnConfigureEndpoints?.Invoke(app);
        }

        return endpoints;
    }
}
