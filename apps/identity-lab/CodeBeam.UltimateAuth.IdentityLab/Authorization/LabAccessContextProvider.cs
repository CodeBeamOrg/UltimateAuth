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
}
