using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public interface IAccessOrchestrator
    {
        Task ExecuteAsync(AccessContext context, IAccessCommand command, CancellationToken ct = default);
    }
}
