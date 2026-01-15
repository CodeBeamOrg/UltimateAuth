using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public interface ISessionCommand<TResult>
    {
        Task<TResult> ExecuteAsync(AuthContext context, ISessionIssuer issuer, CancellationToken ct);
    }
}
