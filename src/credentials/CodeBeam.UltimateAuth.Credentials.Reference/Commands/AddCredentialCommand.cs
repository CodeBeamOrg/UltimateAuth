using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

internal sealed class AddCredentialCommand : IAccessCommand<AddCredentialResult>
{
    private readonly Func<CancellationToken, Task<AddCredentialResult>> _execute;

    public AddCredentialCommand(Func<CancellationToken, Task<AddCredentialResult>> execute)
    {
        _execute = execute;
    }

    public Task<AddCredentialResult> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
}
