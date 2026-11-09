using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.IdentityLab.Authorization;

public sealed class LabAccessContextProvider(IAuthFlow authFlow, IAccessContextFactory contextFactory)
{
    private AuthFlowContext? validatedFlow;

    // Authenticated management contexts must originate in an HTTP endpoint.
    public async Task<AccessContext> CreateAsync(string action, string resource, CancellationToken cancellationToken = default)
    {
        var flow = await authFlow.BeginAsync(AuthFlowType.ValidateSession, cancellationToken);
        validatedFlow = flow;
        return await contextFactory.CreateAsync(flow, action, resource, ct: cancellationToken);
    }


    public Task<AccessContext> CreateForValidatedUserAsync(AccessContext actor, string action, string resource, UserKey targetUserKey, CancellationToken ct = default)
    {
        // Reuse only the flow validated in this request, retaining actor, tenant and device validation.
        if (validatedFlow is not { IsAuthenticated: true } flow ||
            !actor.IsAuthenticated || actor.IsSystemActor ||
            flow.UserKey != actor.ActorUserKey || flow.Tenant != actor.ActorTenant)
            throw new UnauthorizedAccessException("Rollback actor mismatch.");

        return contextFactory.CreateAsync(flow, action, resource, resourceId: targetUserKey.Value, ct: ct);
    }
}
