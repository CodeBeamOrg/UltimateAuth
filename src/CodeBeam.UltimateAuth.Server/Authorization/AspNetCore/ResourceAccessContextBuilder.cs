using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Runtime;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Text.Json;

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
            RoleId[] roleIds;

            var runtime = http.RequestServices.GetRequiredService<IUAuthRuntimeMarker>();

            if (runtime is ResourceRuntimeMarker)
            {
                // Remote session validation has already produced
                // the authenticated principal.
                var identity = http.User.Identities.SingleOrDefault(x => x.IsAuthenticated && x.AuthenticationType == UAuthConstants.SchemeDefaults.GlobalScheme);

                if (identity is null)
                    throw new InvalidOperationException("Trusted UltimateAuth identity is missing.");

                var roles = new List<AccessActorRole>();
                var ids = new List<RoleId>();

                foreach (var claim in identity.FindAll(UAuthConstants.Claims.ActorRole))
                {
                    AccessActorRole? role;

                    try
                    {
                        role = JsonSerializer.Deserialize<AccessActorRole>(claim.Value);
                    }
                    catch (JsonException)
                    {
                        throw new InvalidOperationException("Invalid actor role claim.");
                    }

                    if (role is null ||
                        string.IsNullOrWhiteSpace(role.Name) ||
                        !RoleId.TryParse(role.Id, null, out var roleId))
                    {
                        throw new InvalidOperationException("Invalid actor role claim.");
                    }

                    roles.Add(role);
                    ids.Add(roleId);
                }

                actorRoles = roles;
                roleIds = ids.ToArray();
            }
            else
            {
                var resolver = http.RequestServices.GetRequiredService<AccessActorRoleResolver>();
                var resolved = await resolver.ResolveAsync(tenant, user.UserKey, ct);

                actorRoles = resolved.ActorRoles;
                roleIds = resolved.RoleIds.ToArray();
            }

            attributes = new ReadOnlyDictionary<string, object>(
                new Dictionary<string, object>
                {
                    ["roles"] = roleIds
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
            attributes: attributes);
    }

    private static string ResolveResource(string action)
    {
        var parts = action.Split('.');
        return parts.Length > 0 ? parts[0] : "unknown";
    }
}
