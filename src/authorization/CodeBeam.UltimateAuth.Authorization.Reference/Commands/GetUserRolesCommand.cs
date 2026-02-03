using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Authorization.Reference;

internal sealed class GetUserRolesCommand : IAccessCommand<IReadOnlyCollection<string>>
{
    private readonly Func<CancellationToken, Task<IReadOnlyCollection<string>>> _execute;

    public GetUserRolesCommand(Func<CancellationToken, Task<IReadOnlyCollection<string>>> execute)
    {
        _execute = execute;
    }

    public Task<IReadOnlyCollection<string>> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
}
