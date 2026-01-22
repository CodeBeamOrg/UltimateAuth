using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class ChangeUserIdentifierCommand : IAccessCommand
{
    private readonly Func<CancellationToken, Task> _execute;

    public ChangeUserIdentifierCommand(Func<CancellationToken, Task> execute)
    {
        _execute = execute;
    }

    public Task ExecuteAsync(CancellationToken ct = default) => _execute(ct);
}
