using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public interface ISessionCommand<TUserId, TResult>
    {
        Task<TResult> ExecuteAsync(AuthContext context, ISessionIssuer<TUserId> issuer, CancellationToken ct);
    }
}
