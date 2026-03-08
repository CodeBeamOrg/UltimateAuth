using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Constants;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Policies.Abstractions;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class UAuthAccessOrchestrator : IAccessOrchestrator
{
    private readonly IAccessAuthority _authority;
    private readonly IAccessPolicyProvider _policyProvider;
    private readonly IUserPermissionStore _permissions;

    public UAuthAccessOrchestrator(IAccessAuthority authority, IAccessPolicyProvider policyProvider, IUserPermissionStore permissions)
    {
        _authority = authority;
        _policyProvider = policyProvider;
        _permissions = permissions;
    }

    public async Task ExecuteAsync(AccessContext context, IAccessCommand command, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        context = await EnrichAsync(context, ct);

        var policies = _policyProvider.GetPolicies(context);
        var decision = _authority.Decide(context, policies);

        if (!decision.IsAllowed)
            throw new UAuthAuthorizationException(decision.DenyReason);

        if (decision.RequiresReauthentication)
            throw new InvalidOperationException("Requires reauthentication.");

        await command.ExecuteAsync(ct);
    }

    public async Task<TResult> ExecuteAsync<TResult>(AccessContext context, IAccessCommand<TResult> command, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        context = await EnrichAsync(context, ct);

        var policies = _policyProvider.GetPolicies(context);
        var decision = _authority.Decide(context, policies);

        if (!decision.IsAllowed)
            throw new UAuthAuthorizationException(decision.DenyReason);

        if (decision.RequiresReauthentication)
            throw new InvalidOperationException("Requires reauthentication.");

        return await command.ExecuteAsync(ct);
    }

    private async Task<AccessContext> EnrichAsync(AccessContext context, CancellationToken ct)
    {
        if (context.ActorUserKey is null)
            return context;

        var perms = await _permissions.GetPermissionsAsync(context.ResourceTenant, context.ActorUserKey.Value, ct);
        var compiled = new CompiledPermissionSet(perms);
        return context.WithAttribute(UAuthConstants.Access.Permissions, compiled);
    }
}
