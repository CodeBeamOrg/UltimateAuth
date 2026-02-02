using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

internal sealed class ChangeCredentialCommand: IAccessCommand<ChangeCredentialResult>
{
    private readonly Func<CancellationToken, Task<ChangeCredentialResult>> _execute;

    public ChangeCredentialCommand(Func<CancellationToken, Task<ChangeCredentialResult>> execute)
    {
        _execute = execute;
    }

    public Task<ChangeCredentialResult> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
}
