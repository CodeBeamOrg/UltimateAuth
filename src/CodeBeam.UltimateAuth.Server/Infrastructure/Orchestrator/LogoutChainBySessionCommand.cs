using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed record LogoutChainBySessionCommand(AuthSessionId SessionId) : ISessionCommand<bool>
{
    public async Task<bool> ExecuteAsync(AuthContext context, ISessionIssuer issuer, CancellationToken ct)
    {
        var chainId = await issuer.GetChainIdBySessionAsync(context.Tenant, SessionId, ct);

        if (chainId is null)
            return false;

        await issuer.LogoutChainAsync(context.Tenant, chainId.Value, context.At, ct);

        return true;
    }
}
