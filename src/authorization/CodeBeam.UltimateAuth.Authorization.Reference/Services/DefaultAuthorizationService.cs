using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Authorization.Reference
{
    internal sealed class DefaultAuthorizationService : IAuthorizationService
    {
        private readonly IUserPermissionStore _permissionStore;
        private readonly IAccessAuthority _accessAuthority;

        public DefaultAuthorizationService(IUserPermissionStore permissionStore, IAccessAuthority accessAuthority)
        {
            _permissionStore = permissionStore;
            _accessAuthority = accessAuthority;
        }

        public async Task<AuthorizationResult> AuthorizeAsync(string? tenantId, AccessContext context, CancellationToken ct = default)
        {
            if (context.ActorUserKey is null)
                return AuthorizationResult.Deny("unauthenticated");

            var permissions = await _permissionStore.GetPermissionsAsync(tenantId, context.ActorUserKey.Value, ct);

            var policy = new PermissionAccessPolicy(permissions,context.Action);

            var decision = _accessAuthority.Decide(context, new[] { policy });

            return decision.RequiresReauthentication
                ? AuthorizationResult.ReauthRequired()
                : decision.IsAllowed
                    ? AuthorizationResult.Allow()
                    : AuthorizationResult.Deny(decision.DenyReason);
        }
    }
}
