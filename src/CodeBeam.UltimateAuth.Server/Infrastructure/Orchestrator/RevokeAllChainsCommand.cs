using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class RevokeAllChainsCommand<TUserId> : ISessionCommand<TUserId, Unit>
    {
        public TUserId UserId { get; }
        public ChainId? ExceptChainId { get; }

        public RevokeAllChainsCommand(TUserId userId, ChainId? exceptChainId)
        {
            UserId = userId;
            ExceptChainId = exceptChainId;
        }

        public async Task<Unit> ExecuteAsync(AuthContext context, ISessionIssuer<TUserId> issuer, CancellationToken ct)
        {
            await issuer.RevokeAllChainsAsync(context.TenantId, UserId, ExceptChainId, context.At, ct);
            return Unit.Value;
        }
    }
}
