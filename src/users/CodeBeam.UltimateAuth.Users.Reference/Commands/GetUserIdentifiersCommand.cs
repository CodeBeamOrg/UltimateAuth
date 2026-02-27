using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class GetUserIdentifiersCommand : IAccessCommand<PagedResult<UserIdentifierDto>>
{
    private readonly Func<CancellationToken, Task<PagedResult<UserIdentifierDto>>> _execute;

    public GetUserIdentifiersCommand(Func<CancellationToken, Task<PagedResult<UserIdentifierDto>>> execute)
    {
        _execute = execute;
    }

    public Task<PagedResult<UserIdentifierDto>> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
}
