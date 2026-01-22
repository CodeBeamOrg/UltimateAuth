using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Authorization.Reference
{
    internal sealed class GetUserRolesCommand : IAccessCommand<IReadOnlyCollection<string>>
    {
        private readonly IEnumerable<IAccessPolicy> _policies;
        private readonly Func<CancellationToken, Task<IReadOnlyCollection<string>>> _execute;

        public GetUserRolesCommand(IEnumerable<IAccessPolicy> policies, Func<CancellationToken, Task<IReadOnlyCollection<string>>> execute)
        {
            _policies = policies;
            _execute = execute;
        }

        public IEnumerable<IAccessPolicy> GetPolicies(AccessContext context) => _policies;

        public Task<IReadOnlyCollection<string>> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
    }
}
