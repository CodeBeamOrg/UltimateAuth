using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.InMemory;

namespace CodeBeam.UltimateAuth.Authorization.InMemory;

internal sealed class InMemoryAuthorizationSeedContributor : ISeedContributor
{
    public int Order => 20;

    private readonly IRoleStoreFactory _roleStoreFactory;
    private readonly IUserRoleStoreFactory _userRoleStoreFactory;
    private readonly IInMemoryUserIdProvider<UserKey> _ids;
    private readonly IClock _clock;

    public InMemoryAuthorizationSeedContributor(
        IRoleStoreFactory roleStoreFactory,
        IUserRoleStoreFactory userRoleStoreFactory,
        IInMemoryUserIdProvider<UserKey> ids,
        IClock clock)
    {
        _roleStoreFactory = roleStoreFactory;
        _userRoleStoreFactory = userRoleStoreFactory;
        _ids = ids;
        _clock = clock;
    }

    public async Task SeedAsync(TenantKey tenant, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;

        var roleStore = _roleStoreFactory.Create(tenant);
        var userRoleStore = _userRoleStoreFactory.Create(tenant);

        var adminRole = await roleStore.GetByNameAsync("ADMIN", ct);
        if (adminRole is null)
        {
            adminRole = Role.Create(
                RoleId.From(Guid.Parse("11111111-1111-1111-1111-111111111111")),
                tenant,
                "Admin",
                new HashSet<Permission> { Permission.Wildcard },
                now);

            await roleStore.AddAsync(adminRole, ct);
        }

        var userRole = await roleStore.GetByNameAsync("USER", ct);
        if (userRole is null)
        {
            userRole = Role.Create(
                RoleId.From(Guid.Parse("22222222-2222-2222-2222-222222222222")),
                tenant,
                "User",
                null,
                now);

            await roleStore.AddAsync(userRole, ct);
        }

        var adminKey = _ids.GetAdminUserId();
        await AssignIfMissingAsync(userRoleStore, adminKey, adminRole.Id, now, ct);
        await AssignIfMissingAsync(userRoleStore, adminKey, userRole.Id, now, ct);

        var userKey = _ids.GetUserUserId();
        await AssignIfMissingAsync(userRoleStore, userKey, userRole.Id, now, ct);
    }

    private static async Task AssignIfMissingAsync(
        IUserRoleStore userRoleStore,
        UserKey userKey,
        RoleId roleId,
        DateTimeOffset assignedAt,
        CancellationToken ct)
    {
        var assignments = await userRoleStore.GetAssignmentsAsync(userKey, ct);

        if (assignments.Any(x => x.RoleId == roleId))
            return;

        await userRoleStore.AssignAsync(userKey, roleId, assignedAt, ct);
    }
}
