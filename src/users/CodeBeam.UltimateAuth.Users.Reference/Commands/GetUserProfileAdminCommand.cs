using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class GetUserProfileAdminCommand : IAccessCommand<UserProfileDto>
{
    private readonly IEnumerable<IAccessPolicy> _policies;
    private readonly Func<CancellationToken, Task<UserProfileDto>> _execute;

    public GetUserProfileAdminCommand(IEnumerable<IAccessPolicy> policies, Func<CancellationToken, Task<UserProfileDto>> execute)
    {
        _policies = policies ?? Array.Empty<IAccessPolicy>();
        _execute = execute;
    }

    public IEnumerable<IAccessPolicy> GetPolicies(AccessContext context) => _policies;

    public Task<UserProfileDto> ExecuteAsync(CancellationToken ct = default) => _execute(ct);
}
