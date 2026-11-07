using CodeBeam.UltimateAuth.Server.RateLimiting;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace CodeBeam.UltimateAuth.Server.Extensions;

public static class UAuthHubEndpointExtensions
{
    public static IEndpointRouteBuilder MapUAuthHub(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth/uauthhub");
        group.MapPost("/entry", HandleHub.HandleHubEntry).WithUAuthRateLimit(UAuthEndpoint.HubEntry);

        return endpoints;
    }
}
