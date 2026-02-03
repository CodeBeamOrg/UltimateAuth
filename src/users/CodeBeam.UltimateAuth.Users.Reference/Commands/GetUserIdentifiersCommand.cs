using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class GetUserIdentifiersCommand : IAccessCommand<IReadOnlyList<UserIdentifierDto>>
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<UserIdentifierDto>>> _execute;

    public GetUserIdentifiersCommand(Func<CancellationToken, Task<IReadOnlyList<UserIdentifierDto>>> execute)
    {
        _execute = execute;
    }

    public Task<IReadOnlyList<UserIdentifierDto>> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
}
