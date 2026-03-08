using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Authorization.Domain;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Policies.Abstractions;

namespace CodeBeam.UltimateAuth.Authorization.Reference;

internal sealed class AuthorizationService : IAuthorizationService
{
    private readonly IUserPermissionStore _permissions;
    private readonly IAccessPolicyProvider _policyProvider;
    private readonly IAccessAuthority _accessAuthority;

    public AuthorizationService(IUserPermissionStore permissions, IAccessPolicyProvider policyProvider, IAccessAuthority accessAuthority)
    {
        _permissions = permissions;
        _policyProvider = policyProvider;
        _accessAuthority = accessAuthority;
    }

    public async Task<AuthorizationResult> AuthorizeAsync(AccessContext context, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        IReadOnlyCollection<Permission> permissions = Array.Empty<Permission>();

        if (context.ActorUserKey is not null)
        {
            permissions = await _permissions.GetPermissionsAsync(context.ResourceTenant, context.ActorUserKey.Value, ct);
        }

        var enrichedContext = context.WithAttribute("permissions", permissions);

        var policies = _policyProvider.GetPolicies(enrichedContext);
        var decision = _accessAuthority.Decide(enrichedContext, policies);

        if (decision.RequiresReauthentication)
            return AuthorizationResult.ReauthRequired();

        return decision.IsAllowed
            ? AuthorizationResult.Allow()
            : AuthorizationResult.Deny(decision.DenyReason);
    }
}
