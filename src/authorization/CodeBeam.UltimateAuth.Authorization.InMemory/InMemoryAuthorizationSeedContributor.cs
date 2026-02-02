using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization.InMemory;

internal sealed class InMemoryAuthorizationSeedContributor : ISeedContributor
{
    public int Order => 20;

    private readonly IUserRoleStore _roles;
    private readonly IInMemoryUserIdProvider<UserKey> _ids;

    public InMemoryAuthorizationSeedContributor(IUserRoleStore roles, IInMemoryUserIdProvider<UserKey> ids)
    {
        _roles = roles;
        _ids = ids;
    }

    public async Task SeedAsync(TenantKey tenant, CancellationToken ct = default)
    {
        var adminKey = _ids.GetAdminUserId();

        await _roles.AssignAsync(tenant, adminKey, "Admin", ct);
    }
}
