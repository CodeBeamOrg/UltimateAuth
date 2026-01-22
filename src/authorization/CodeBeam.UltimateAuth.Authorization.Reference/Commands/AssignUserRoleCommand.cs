using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Authorization.Reference
{
    internal sealed class AssignUserRoleCommand : IAccessCommand
    {
        private readonly Func<CancellationToken, Task> _execute;
        private readonly IEnumerable<IAccessPolicy> _policies;

        public AssignUserRoleCommand(IEnumerable<IAccessPolicy> policies, Func<CancellationToken, Task> execute)
        {
            _policies = policies;
            _execute = execute;
        }

        public IEnumerable<IAccessPolicy> GetPolicies(AccessContext context) => _policies;

        public Task ExecuteAsync(CancellationToken ct = default) => _execute(ct);
    }
}
