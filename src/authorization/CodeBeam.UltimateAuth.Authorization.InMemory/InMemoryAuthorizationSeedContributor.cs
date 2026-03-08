using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Authorization.Domain;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization.InMemory;

internal sealed class InMemoryAuthorizationSeedContributor : ISeedContributor
{
    public int Order => 20;

    private readonly IRoleStore _roleStore;
    private readonly IUserRoleStore _roles;
    private readonly IInMemoryUserIdProvider<UserKey> _ids;
    private readonly IClock _clock;

    public InMemoryAuthorizationSeedContributor(IRoleStore roleStore, IUserRoleStore roles, IInMemoryUserIdProvider<UserKey> ids, IClock clock)
    {
        _roleStore = roleStore;
        _roles = roles;
        _ids = ids;
        _clock = clock;
    }

    public async Task SeedAsync(TenantKey tenant, CancellationToken ct = default)
    {
        var adminRoleId = RoleId.From(Guid.NewGuid());
        var userRoleId = RoleId.From(Guid.NewGuid());
        await _roleStore.AddAsync(Role.Create(adminRoleId, tenant, "Admin", new HashSet<Permission>() { Permission.Wildcard }, _clock.UtcNow));
        await _roleStore.AddAsync(Role.Create(userRoleId, tenant, "User", null, _clock.UtcNow));

        var adminKey = _ids.GetAdminUserId();
        await _roles.AssignAsync(tenant, adminKey, adminRoleId, ct);
        await _roles.AssignAsync(tenant, adminKey, userRoleId, ct);

        var userKey = _ids.GetUserUserId();
        await _roles.AssignAsync(tenant, userKey, userRoleId, ct);
    }
}
