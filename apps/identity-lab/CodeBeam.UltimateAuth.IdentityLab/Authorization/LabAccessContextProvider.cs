using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;

namespace CodeBeam.UltimateAuth.IdentityLab.Authorization;

public sealed class LabAccessContextProvider(IAuthFlow authFlow, IAccessContextFactory contextFactory)
{
    public async Task<AccessContext> CreateAsync(string action, string resource, CancellationToken cancellationToken = default)
    {
        var flow = await authFlow.BeginAsync(AuthFlowType.ValidateSession, cancellationToken);
        return await contextFactory.CreateAsync(flow, action, resource, ct: cancellationToken);
    }


    public async Task<AccessContext> CreateForUserAsync(string action, string resource, UserKey targetUserKey, CancellationToken ct = default)
    {
        var flow = await authFlow.BeginAsync(AuthFlowType.ValidateSession, ct);
        return await contextFactory.CreateAsync(flow, action, resource, resourceId: targetUserKey.Value, ct: ct);
    }
}
