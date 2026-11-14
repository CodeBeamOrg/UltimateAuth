using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.IdentityLab.Authorization;

public sealed class LabAccessContextProvider(IAuthFlow authFlow, IAccessContextFactory contextFactory)
{
    // Authenticated management contexts must originate in an HTTP endpoint.
    public async Task<AccessContext> CreateAsync(string action, string resource, CancellationToken cancellationToken = default)
    {
        var validated = await CreateValidatedAsync(action, resource, cancellationToken);
        return validated.Context;
    }

    public async Task<(AuthFlowContext Flow, AccessContext Context)> CreateValidatedAsync(string action, string resource, CancellationToken cancellationToken = default)
    {
        var flow = await authFlow.BeginAsync(AuthFlowType.ValidateSession, cancellationToken);
        var context = await contextFactory.CreateAsync(flow, action, resource, ct: cancellationToken);
        return (flow, context);
    }


    public Task<AccessContext> CreateForValidatedUserAsync(AuthFlowContext flow, AccessContext actor, string action, string resource, UserKey targetUserKey, CancellationToken ct = default)
    {
        // Reuse only the flow validated in this request, retaining actor, tenant and device validation.
        if (!flow.IsAuthenticated || flow.UserKey is null ||
            !actor.IsAuthenticated || actor.IsSystemActor ||
            flow.UserKey != actor.ActorUserKey || flow.Tenant != actor.ActorTenant || actor.IsCrossTenant)
            throw new UnauthorizedAccessException("Rollback actor mismatch.");

        return contextFactory.CreateAsync(flow, action, resource, resourceId: targetUserKey.Value, ct: ct);
    }
}
