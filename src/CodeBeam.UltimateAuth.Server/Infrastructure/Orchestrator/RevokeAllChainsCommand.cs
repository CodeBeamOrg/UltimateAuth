using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class RevokeAllChainsCommand : ISessionCommand<Unit>
    {
        public UserKey UserKey { get; }
        public SessionChainId? ExceptChainId { get; }

        public RevokeAllChainsCommand(UserKey userKey, SessionChainId? exceptChainId)
        {
            UserKey = userKey;
            ExceptChainId = exceptChainId;
        }

        public async Task<Unit> ExecuteAsync(AuthContext context, ISessionIssuer issuer, CancellationToken ct)
        {
            await issuer.RevokeAllChainsAsync(context.TenantId, UserKey, ExceptChainId, context.At, ct);
            return Unit.Value;
        }
    }
}
