using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Routing;

namespace CodeBeam.UltimateAuth.Server.Endpoints;

public interface IAuthEndpointRegistrar
{
    void MapEndpoints(RouteGroupBuilder rootGroup, UAuthServerOptions options);
}
