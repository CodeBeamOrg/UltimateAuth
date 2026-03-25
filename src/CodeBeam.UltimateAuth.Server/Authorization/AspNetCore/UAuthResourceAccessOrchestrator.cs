using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Policies.Abstractions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Authorization;

public sealed class UAuthResourceAccessOrchestrator : IAccessOrchestrator
{
    private readonly IAccessAuthority _authority;
    private readonly IAccessPolicyProvider _policyProvider;
    private readonly IHttpContextAccessor _http;

    public UAuthResourceAccessOrchestrator(
        IAccessAuthority authority,
        IAccessPolicyProvider policyProvider,
        IHttpContextAccessor http)
    {
        _authority = authority;
        _policyProvider = policyProvider;
        _http = http;
    }

    public async Task ExecuteAsync(AccessContext context, IAccessCommand command, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        context = EnrichFromClaims(context);

        var policies = _policyProvider.GetPolicies(context);
        var decision = _authority.Decide(context, policies);

        if (!decision.IsAllowed)
            throw new UAuthAuthorizationException(decision.DenyReason ?? "authorization_denied");

        if (decision.RequiresReauthentication)
            throw new InvalidOperationException("Requires reauthentication.");

        await command.ExecuteAsync(ct);
    }

    public async Task<TResult> ExecuteAsync<TResult>(AccessContext context, IAccessCommand<TResult> command, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        context = EnrichFromClaims(context);

        var policies = _policyProvider.GetPolicies(context);
        var decision = _authority.Decide(context, policies);

        if (!decision.IsAllowed)
            throw new UAuthAuthorizationException(decision.DenyReason ?? "authorization_denied");

        if (decision.RequiresReauthentication)
            throw new InvalidOperationException("Requires reauthentication.");

        return await command.ExecuteAsync(ct);
    }

    private AccessContext EnrichFromClaims(AccessContext context)
    {
        var http = _http.HttpContext!;
        var user = http.User;

        var permissions = user.Claims
            .Where(c => c.Type == "uauth:permission")
            .Select(c => Permission.From(c.Value));

        var compiled = new CompiledPermissionSet(permissions);

        return context.WithAttribute(UAuthConstants.Access.Permissions, compiled);
    }
}
