using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed record RevokeSessionCommand(AuthSessionId SessionId) : ISessionCommand<bool>
{
    public async Task<bool> ExecuteAsync(AuthContext context, ISessionIssuer issuer, CancellationToken ct)
    {
        return await issuer.RevokeSessionAsync(context.Tenant, SessionId, context.At, ct);
    }
}
