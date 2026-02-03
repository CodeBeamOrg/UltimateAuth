using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Authorization.Reference;

internal sealed class AssignUserRoleCommand : IAccessCommand
{
    private readonly Func<CancellationToken, Task> _execute;

    public AssignUserRoleCommand(Func<CancellationToken, Task> execute)
    {
        _execute = execute;
    }

    public Task ExecuteAsync(CancellationToken ct = default) => _execute(ct);
}
