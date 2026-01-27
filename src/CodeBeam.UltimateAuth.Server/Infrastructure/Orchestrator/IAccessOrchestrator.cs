using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public interface IAccessOrchestrator
    {
        Task ExecuteAsync(AccessContext context, IAccessCommand command, CancellationToken ct = default);
        Task<TResult> ExecuteAsync<TResult>(AccessContext context, IAccessCommand<TResult> command, CancellationToken ct = default);
    }
}
