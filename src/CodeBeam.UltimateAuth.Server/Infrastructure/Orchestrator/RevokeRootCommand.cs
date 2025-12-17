using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure.Orchestrator
{
    public sealed class RevokeRootCommand<TUserId> : ISessionCommand<TUserId, Unit>
    {
        public TUserId UserId { get; }

        public RevokeRootCommand(TUserId userId)
        {
            UserId = userId;
        }

        public async Task<Unit> ExecuteAsync(
            AuthContext context,
            ISessionIssuer<TUserId> issuer,
            CancellationToken ct)
        {
            await issuer.RevokeRootAsync(
                context.TenantId,
                UserId,
                context.At,
                ct);

            return Unit.Value;
        }
    }
}
