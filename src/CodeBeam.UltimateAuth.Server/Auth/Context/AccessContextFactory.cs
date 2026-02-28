using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Collections.ObjectModel;

namespace CodeBeam.UltimateAuth.Server.Auth;

internal sealed class AccessContextFactory : IAccessContextFactory
{
    private readonly IUserRoleStore _roleStore;
    private readonly IUserIdConverterResolver _converterResolver;

    public AccessContextFactory(IUserRoleStore roleStore, IUserIdConverterResolver converterResolver)
    {
        _roleStore = roleStore;
        _converterResolver = converterResolver;
    }

    public async Task<AccessContext> CreateAsync(AuthFlowContext authFlow, string action, string resource, string? resourceId = null, IDictionary<string, object>? attributes = null, CancellationToken ct = default)
    {
        return await CreateInternalAsync(authFlow, action, resource, authFlow.Tenant, resourceId, attributes, ct);
    }

    public async Task<AccessContext> CreateForExplicitTenantResourceAsync(AuthFlowContext authFlow, string action, string resource, TenantKey resourceTenant, string? resourceId = null, IDictionary<string, object>? attributes = null, CancellationToken ct = default)
    {
        if (resourceTenant.IsSystem || resourceTenant.IsUnresolved)
            throw new InvalidOperationException("Invalid resource tenant.");

        return await CreateInternalAsync(authFlow, action, resource, resourceTenant, resourceId, attributes, ct);
    }

    private async Task<AccessContext> CreateInternalAsync(AuthFlowContext authFlow, string action, string resource, TenantKey resourceTenant, string? resourceId, IDictionary<string, object>? attributes, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required.", nameof(action));

        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Resource is required.", nameof(resource));

        var attrs = attributes != null
            ? new Dictionary<string, object>(attributes)
            : new Dictionary<string, object>();

        if (authFlow.IsAuthenticated && authFlow.UserKey is not null)
        {
            var roles = await _roleStore.GetRolesAsync(authFlow.Tenant, authFlow.UserKey.Value, ct);
            attrs["roles"] = roles;
        }

        UserKey? targetUserKey = null;

        if (!string.IsNullOrWhiteSpace(resourceId))
        {
            var converter = _converterResolver.GetConverter<UserKey>(null);

            if (!converter.TryFromString(resourceId, out var parsed))
                throw new InvalidOperationException("Invalid resource user id.");

            var canonical = converter.ToCanonicalString(parsed);
            targetUserKey = UserKey.FromString(canonical);
        }

        return new AccessContext(
            actorUserKey: authFlow.UserKey,
            actorTenant: authFlow.Tenant,
            isAuthenticated: authFlow.IsAuthenticated,
            isSystemActor: authFlow.Tenant.IsSystem,
            actorChainId: authFlow.Session?.ChainId,
            resource: resource,
            targetUserKey: targetUserKey,
            resourceTenant: resourceTenant,
            action: action,
            attributes: attrs.Count > 0
                ? new ReadOnlyDictionary<string, object>(attrs)
                : EmptyAttributes.Instance
        );
    }
}
