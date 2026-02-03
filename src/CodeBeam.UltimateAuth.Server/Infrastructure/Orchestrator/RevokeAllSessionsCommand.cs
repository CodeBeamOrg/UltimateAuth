using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class RevokeAllUserSessionsCommand : ISessionCommand<Unit>
{
    public UserKey UserKey { get; }

    public RevokeAllUserSessionsCommand(UserKey userKey)
    {
        UserKey = userKey;
    }

    // TODO: This method should call its own logic. Not revoke root.
    public async Task<Unit> ExecuteAsync(AuthContext context, ISessionIssuer issuer, CancellationToken ct)
    {
        await issuer.RevokeRootAsync(context.Tenant, UserKey, context.At, ct);
        return Unit.Value;
    }

}
