using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

internal sealed class DeleteCredentialCommand : IAccessCommand<CredentialActionResult>
{
    private readonly IEnumerable<IAccessPolicy> _policies;
    private readonly Func<CancellationToken, Task<CredentialActionResult>> _execute;

    public DeleteCredentialCommand(IEnumerable<IAccessPolicy> policies, Func<CancellationToken, Task<CredentialActionResult>> execute)
    {
        _policies = policies ?? Array.Empty<IAccessPolicy>();
        _execute = execute;
    }

    public IEnumerable<IAccessPolicy> GetPolicies(AccessContext context) => _policies;

    public Task<CredentialActionResult> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
}
