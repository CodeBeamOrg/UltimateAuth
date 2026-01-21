using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public interface IAccessCommand
    {
        IEnumerable<IAccessPolicy> GetPolicies(AccessContext context);
        Task ExecuteAsync(CancellationToken ct = default);
    }

    // For get commands
    public interface IAccessCommand<TResult>
    {
        IEnumerable<IAccessPolicy> GetPolicies(AccessContext context);
        Task<TResult> ExecuteAsync(CancellationToken ct = default);
    }

}
