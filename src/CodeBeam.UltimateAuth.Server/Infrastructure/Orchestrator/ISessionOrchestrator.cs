using CodeBeam.UltimateAuth.Core.Contracts;
namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    internal interface ISessionOrchestrator<TUserId>
    {
        Task<TResult> ExecuteAsync<TResult>(AuthContext authContext, ISessionCommand<TUserId, TResult> command, CancellationToken ct = default);
    }
}
