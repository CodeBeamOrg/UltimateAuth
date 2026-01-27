using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Core.Contracts;
using System.Collections.ObjectModel;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    internal sealed class DefaultAccessContextFactory : IAccessContextFactory
    {
        private readonly IUserRoleStore _roleStore;

        public DefaultAccessContextFactory(IUserRoleStore roleStore)
        {
            _roleStore = roleStore;
        }

        public async Task<AccessContext> CreateAsync(AuthFlowContext authFlow, string action, string resource, string? resourceId = null, string? resourceTenantId = null, IDictionary<string, object>? attributes = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("Action is required.", nameof(action));

            if (string.IsNullOrWhiteSpace(resource))
                throw new ArgumentException("Resource is required.", nameof(resource));

            var attrs = attributes is not null
                ? new Dictionary<string, object>(attributes)
                : new Dictionary<string, object>();

            if (authFlow.IsAuthenticated && authFlow.UserKey is not null)
            {
                var roles = await _roleStore.GetRolesAsync(authFlow.TenantId, authFlow.UserKey.Value, ct);
                attrs["roles"] = roles;
            }

            return new AccessContext
            {
                ActorUserKey = authFlow.UserKey,
                ActorTenantId = authFlow.TenantId,
                IsAuthenticated = authFlow.IsAuthenticated,
                IsSystemActor = false,

                Resource = resource,
                ResourceId = resourceId,
                ResourceTenantId = resourceTenantId ?? authFlow.TenantId,

                Action = action,

                Attributes = attrs.Count > 0
                    ? new ReadOnlyDictionary<string, object>(attrs)
                    : EmptyAttributes.Instance
            };
        }
    }
}
