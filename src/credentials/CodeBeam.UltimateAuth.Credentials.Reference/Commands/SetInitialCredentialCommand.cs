using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Credentials.Reference
{
    internal sealed class SetInitialCredentialCommand : IAccessCommand
    {
        private readonly IEnumerable<IAccessPolicy> _policies;
        private readonly Func<CancellationToken, Task> _execute;

        public SetInitialCredentialCommand(IEnumerable<IAccessPolicy> policies, Func<CancellationToken, Task> execute)
        {
            _policies = policies ?? Array.Empty<IAccessPolicy>();
            _execute = execute;
        }

        public IEnumerable<IAccessPolicy> GetPolicies(AccessContext context) => _policies;

        public Task ExecuteAsync(CancellationToken ct = default) => _execute(ct);
    }
}
