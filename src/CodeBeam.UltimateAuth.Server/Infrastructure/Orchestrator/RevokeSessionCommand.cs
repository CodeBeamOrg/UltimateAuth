using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed record RevokeSessionCommand(AuthSessionId SessionId) : ISessionCommand<Unit>
{
    public async Task<Unit> ExecuteAsync(AuthContext context, ISessionIssuer issuer, CancellationToken ct)
    {
        await issuer.RevokeSessionAsync(context.TenantId, SessionId, context.At, ct);
        return Unit.Value;
    }
}
