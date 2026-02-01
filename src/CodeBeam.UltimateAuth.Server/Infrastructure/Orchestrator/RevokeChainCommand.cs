using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure.Orchestrator
{
    public sealed class RevokeChainCommand : ISessionCommand<Unit>
    {
        public SessionChainId ChainId { get; }

        public RevokeChainCommand(SessionChainId chainId)
        {
            ChainId = chainId;
        }

        public async Task<Unit> ExecuteAsync(AuthContext context, ISessionIssuer issuer, CancellationToken ct)
        {
            await issuer.RevokeChainAsync(context.TenantId, ChainId, context.At, ct);
            return Unit.Value;
        }
    }
}
