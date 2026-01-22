using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class GetUserProfileAdminCommand : IAccessCommand<UserProfileDto>
{
    private readonly Func<CancellationToken, Task<UserProfileDto>> _execute;

    public GetUserProfileAdminCommand(Func<CancellationToken, Task<UserProfileDto>> execute)
    {
        _execute = execute;
    }

    public Task<UserProfileDto> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
}
