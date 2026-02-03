using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class UserIdentifierExistsCommand : IAccessCommand<bool>
{
    private readonly Func<CancellationToken, Task<bool>> _execute;

    public UserIdentifierExistsCommand(Func<CancellationToken, Task<bool>> execute)
    {
        _execute = execute;
    }

    public Task<bool> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
}
