using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Credentials.Reference
{
    internal sealed class GetAllCredentialsCommand : IAccessCommand<GetCredentialsResult>
    {
        private readonly IEnumerable<IAccessPolicy> _policies;
        private readonly Func<CancellationToken, Task<GetCredentialsResult>> _execute;

        public GetAllCredentialsCommand(IEnumerable<IAccessPolicy> policies, Func<CancellationToken, Task<GetCredentialsResult>> execute)
        {
            _policies = policies ?? Array.Empty<IAccessPolicy>();
            _execute = execute;
        }

        public IEnumerable<IAccessPolicy> GetPolicies(AccessContext context)=> _policies;

        public Task<GetCredentialsResult> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
    }
}
