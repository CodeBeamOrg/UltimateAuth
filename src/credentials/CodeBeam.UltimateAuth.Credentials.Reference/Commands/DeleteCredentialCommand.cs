using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

internal sealed class DeleteCredentialCommand : IAccessCommand<CredentialActionResult>
{
    private readonly Func<CancellationToken, Task<CredentialActionResult>> _execute;

    public DeleteCredentialCommand(Func<CancellationToken, Task<CredentialActionResult>> execute)
    {
        _execute = execute;
    }

    public Task<CredentialActionResult> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
}
