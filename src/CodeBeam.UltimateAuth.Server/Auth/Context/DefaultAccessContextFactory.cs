using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    internal sealed class DefaultAccessContextFactory : IAccessContextFactory
    {
        public AccessContext Create(AuthFlowContext authFlow, string action, string resource, string? resourceId = null, string? resourceTenantId = null, IReadOnlyDictionary<string, object>? attributes = null)
        {
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("Action is required.", nameof(action));

            if (string.IsNullOrWhiteSpace(resource))
                throw new ArgumentException("Resource is required.", nameof(resource));

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

                Attributes = attributes ?? EmptyAttributes.Instance
            };
        }
    }
}
