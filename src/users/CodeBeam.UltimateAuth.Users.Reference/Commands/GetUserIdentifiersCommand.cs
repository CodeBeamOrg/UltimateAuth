using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    internal sealed class GetUserIdentifiersCommand : IAccessCommand<GetUserIdentifiersResult>
    {
        private readonly Func<CancellationToken, Task<GetUserIdentifiersResult>> _execute;
        private readonly IEnumerable<IAccessPolicy> _policies;

        public GetUserIdentifiersCommand(IEnumerable<IAccessPolicy> policies, Func<CancellationToken, Task<GetUserIdentifiersResult>> execute)
        {
            _policies = policies ?? Array.Empty<IAccessPolicy>();
            _execute = execute;
        }

        public IEnumerable<IAccessPolicy> GetPolicies(AccessContext context) => _policies;

        public Task<GetUserIdentifiersResult> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
    }
}
