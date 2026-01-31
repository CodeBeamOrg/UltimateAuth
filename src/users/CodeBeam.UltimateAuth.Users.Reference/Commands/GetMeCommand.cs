using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class GetMeCommand : IAccessCommand<UserViewDto>
{
    private readonly Func<CancellationToken, Task<UserViewDto>> _execute;

    public GetMeCommand(Func<CancellationToken, Task<UserViewDto>> execute)
    {
        _execute = execute;
    }

    public Task<UserViewDto> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
}
