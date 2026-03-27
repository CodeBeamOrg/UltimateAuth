using CodeBeam.UltimateAuth.Core.Runtime;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapUltimateAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var sp = endpoints.ServiceProvider;

        var registrar = sp.GetRequiredService<IAuthEndpointRegistrar>();
        var options = sp.GetRequiredService<IOptions<UAuthServerOptions>>().Value;

        var marker = sp.GetService<IUAuthHubMarker>();
        var requiresCors = marker?.RequiresCors == true;

        var rootGroup = endpoints.MapGroup("");

        if (requiresCors)
        {
            rootGroup = rootGroup.RequireCors("UAuthHub");
        }

        registrar.MapEndpoints(rootGroup, options);

        if (endpoints is WebApplication app)
        {
            options.OnConfigureEndpoints?.Invoke(app);
        }

        return endpoints;
    }
}
