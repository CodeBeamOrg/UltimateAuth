using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure.Orchestrator
{
    public sealed class RevokeRootCommand : ISessionCommand<Unit>
    {
        public UserKey UserKey { get; }

        public RevokeRootCommand(UserKey userKey)
        {
            UserKey = userKey;
        }

        public async Task<Unit> ExecuteAsync(
            AuthContext context,
            ISessionIssuer issuer,
            CancellationToken ct)
        {
            await issuer.RevokeRootAsync(
                context.TenantId,
                UserKey,
                context.At,
                ct);

            return Unit.Value;
        }
    }
}
