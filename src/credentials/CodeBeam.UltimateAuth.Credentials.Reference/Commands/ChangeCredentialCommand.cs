using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

internal sealed class ChangeCredentialCommand: IAccessCommand<ChangeCredentialResult>
{
    private readonly IEnumerable<IAccessPolicy> _policies;
    private readonly Func<CancellationToken, Task<ChangeCredentialResult>> _execute;

    public ChangeCredentialCommand(IEnumerable<IAccessPolicy> policies, Func<CancellationToken, Task<ChangeCredentialResult>> execute)
    {
        _policies = policies ?? Array.Empty<IAccessPolicy>();
        _execute = execute;
    }

    public IEnumerable<IAccessPolicy> GetPolicies(AccessContext context) => _policies;

    public Task<ChangeCredentialResult> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
}
