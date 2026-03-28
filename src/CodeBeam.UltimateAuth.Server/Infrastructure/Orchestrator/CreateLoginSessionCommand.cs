using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed record CreateLoginSessionCommand(SessionIssuanceContext LoginContext) : ISessionCommand<IssuedSession>
{
    public Task<IssuedSession> ExecuteAsync(AuthContext _, ISessionIssuer issuer, CancellationToken ct)
    {
        return issuer.IssueSessionAsync(LoginContext, ct);
    }
}
