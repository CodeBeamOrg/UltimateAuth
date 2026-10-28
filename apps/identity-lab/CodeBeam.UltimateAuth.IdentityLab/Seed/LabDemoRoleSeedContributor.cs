using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;

namespace CodeBeam.UltimateAuth.IdentityLab.Seed;

public sealed class LabDemoRoleSeedContributor(IRoleStoreFactory roleStoreFactory, IClock clock) : ISeedContributor
{
    public int Order => 10;

    public async Task SeedAsync(TenantKey tenant, CancellationToken ct = default)
    {
        var roleStore = roleStoreFactory.Create(tenant);

        var definitions = new[]
        {
            (
                Name: LabRoles.Manager,
                Id: LabRoleIds.Manager,
                Permissions: LabDemoPermissions.Manager
            ),
            (
                Name: LabRoles.Customer,
                Id: LabRoleIds.Customer,
                Permissions: LabDemoPermissions.Customer
            )
        };

        foreach (var definition in definitions)
        {
            var existing = await roleStore.GetByNameAsync(definition.Name.ToUpperInvariant(), ct);

            if (existing is not null)
            {
                if (existing.Id != definition.Id || existing.IsDeleted)
                {
                    throw new InvalidOperationException($"Demo role '{definition.Name}' has an invalid definition.");
                }

                var expectedPermissions = definition.Permissions.Select(Permission.From).ToHashSet();

                if (!existing.Permissions.ToHashSet()
                    .SetEquals(expectedPermissions))
                {
                    var expectedVersion = existing.Version;

                    existing.SetPermissions(
                        expectedPermissions,
                        clock.UtcNow);

                    await roleStore.SaveAsync(
                        existing,
                        expectedVersion,
                        ct);
                }

                continue;
            }

            var permissions = definition.Permissions.Select(Permission.From).ToHashSet();
            var role = Role.Create(definition.Id, tenant, definition.Name, permissions, clock.UtcNow);

            await roleStore.AddAsync(role, ct);

        }
    }
}
