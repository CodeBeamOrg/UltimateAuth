using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Authorization.Reference;

internal sealed class RemoveUserRoleCommand : IAccessCommand
{
    private readonly Func<CancellationToken, Task> _execute;

    public RemoveUserRoleCommand(Func<CancellationToken, Task> execute)
    {
        _execute = execute;
    }

    public Task ExecuteAsync(CancellationToken ct = default) => _execute(ct);
}
