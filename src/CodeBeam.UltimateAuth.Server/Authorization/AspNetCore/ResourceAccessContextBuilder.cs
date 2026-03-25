using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Server.Authorization;

internal static class ResourceAccessContextBuilder
{
    public static AccessContext Create(HttpContext http, string action)
    {
        var user = http.RequestServices.GetRequiredService<ICurrentUser>();

        return new AccessContext(
            actorUserKey: user.IsAuthenticated ? user.UserKey : null,
            actorTenant: http.GetTenant(),
            isAuthenticated: user.IsAuthenticated,
            isSystemActor: false,
            actorChainId: null,

            resource: ResolveResource(action),
            targetUserKey: null,
            resourceTenant: http.GetTenant(),

            action: action,
            attributes: EmptyAttributes.Instance
        );
    }

    private static string ResolveResource(string action)
    {
        var parts = action.Split('.');
        return parts.Length > 0 ? parts[0] : "unknown";
    }
}
