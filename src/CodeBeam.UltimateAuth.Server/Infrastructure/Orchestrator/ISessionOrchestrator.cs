using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal interface ISessionOrchestrator
{
    Task<TResult> ExecuteAsync<TResult>(AuthContext authContext, ISessionCommand<TResult> command, CancellationToken ct = default);
}
