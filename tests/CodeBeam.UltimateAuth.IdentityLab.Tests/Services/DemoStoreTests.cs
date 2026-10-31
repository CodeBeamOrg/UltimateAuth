using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using Moq;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Services;

public sealed class DemoStoreTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Demo_store_only_accepts_exact_demo_permissions_from_fixed_role_ids(bool customer)
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var id = customer ? LabRoleIds.Customer : LabRoleIds.Manager;
        var expected = customer ? LabDemoPermissions.Customer : LabDemoPermissions.Manager;
        var userStore = new Mock<IUserRoleStore>();
        userStore.Setup(x => x.GetAssignmentsAsync(LabDatabase.MemberA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new UserRole { RoleId = id }, new UserRole { RoleId = LabRoleIds.Admin } });
        var users = new Mock<IUserRoleStoreFactory>();
        users.Setup(x => x.Create(It.IsAny<TenantKey>())).Returns(userStore.Object);
        var store = new Mock<IRoleStore>();
        // Deliberately hostile store response: wildcard, admin permission and an
        // unrelated role must all be filtered even if the backing data is corrupt.
        var permissions = expected.Select(Permission.From).Append(Permission.Wildcard)
            .Append(Permission.From("users.delete.admin")).ToHashSet();
        store.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<RoleId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] {
                Role.Create(id, TenantKeys.Single, customer ? "Customer" : "Manager", permissions, DateTimeOffset.UnixEpoch),
                Role.Create(LabRoleIds.Admin, TenantKeys.Single, "Ladmin", permissions, DateTimeOffset.UnixEpoch)
            });
        var roles = new Mock<IRoleStoreFactory>();
        roles.Setup(x => x.Create(It.IsAny<TenantKey>())).Returns(store.Object);
        var service = new LabDemoStoreService(ActorProvider.For(LabDatabase.MemberA), db, roles.Object, users.Object);
        var actual = await service.GetPermissionsAsync();
        Assert.True(actual.SetEquals(expected));
        await service.RequireAsync(LabDemoPermissions.ProductsRead);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RequireAsync("users.delete.admin"));
        await db.SetStatusAsync(SandboxStatus.Suspended);
        Assert.Empty(await service.GetPermissionsAsync());
    }
}
