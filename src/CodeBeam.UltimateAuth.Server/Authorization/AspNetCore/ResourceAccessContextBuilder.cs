using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace CodeBeam.UltimateAuth.Server.Authorization;

internal static class ResourceAccessContextBuilder
{
    public static async Task<AccessContext> CreateAsync(HttpContext http, string action, CancellationToken ct = default)
    {
        var user = http.RequestServices.GetRequiredService<ICurrentUser>();

        var tenant = http.GetTenant();

        IReadOnlyList<AccessActorRole> actorRoles = [];
        IReadOnlyDictionary<string, object> attributes = EmptyAttributes.Instance;

        if (user.IsAuthenticated)
        {
            var resolver = http.RequestServices.GetRequiredService<AccessActorRoleResolver>();
            var resolved = await resolver.ResolveAsync(tenant, user.UserKey, ct);
            actorRoles = resolved.ActorRoles;

            attributes = new ReadOnlyDictionary<string, object>(
                new Dictionary<string, object>
                {
                    ["roles"] = resolved.RoleIds.ToArray()
                });
        }

        return new AccessContext(
            actorUserKey: user.IsAuthenticated ? user.UserKey : null,
            actorTenant: tenant,
            isAuthenticated: user.IsAuthenticated,
            isSystemActor: false,
            actorChainId: null,
            actorRoles: actorRoles,
            resource: ResolveResource(action),
            targetUserKey: null,
            resourceTenant: tenant,
            action: action,
            attributes: attributes
        );
    }

    private static string ResolveResource(string action)
    {
        var parts = action.Split('.');
        return parts.Length > 0 ? parts[0] : "unknown";
    }
}
