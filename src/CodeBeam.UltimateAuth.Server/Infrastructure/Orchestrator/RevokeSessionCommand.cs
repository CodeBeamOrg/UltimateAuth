using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal sealed record RevokeSessionCommand<TUserId>(string? TenantId, AuthSessionId SessionId) : ISessionCommand<TUserId, Unit>
    {
        public async Task<Unit> ExecuteAsync(AuthContext _, ISessionIssuer<TUserId> issuer, CancellationToken ct)
        {
            await issuer.RevokeSessionAsync(TenantId, SessionId, _.At, ct);
            return Unit.Value;
        }
    }
}
