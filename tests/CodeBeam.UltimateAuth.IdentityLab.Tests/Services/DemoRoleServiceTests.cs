using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using Moq;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Services;

public sealed class DemoRoleServiceTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData("Ladmin")]
    [InlineData("lAdMiN")]
    [InlineData("Owner")]
    [InlineData("User")]
    [InlineData(" Manager")]
    [InlineData("Manager\0")]
    public async Task Demo_operations_reject_system_and_forged_names_without_accessing_stores(string role)
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var before = await db.SnapshotAsync();
        var roles = new Mock<IRoleStoreFactory>(MockBehavior.Strict);
        var assignments = new Mock<IUserRoleStoreFactory>(MockBehavior.Strict);
        var service = new LabDemoRoleService(ActorProvider.For(LabDatabase.OwnerA), db, roles.Object, assignments.Object);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SetRoleAsync(LabDatabase.MemberA, role, true));
        Assert.Equal(before, await db.SnapshotAsync());
        roles.VerifyNoOtherCalls();
        assignments.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("owner-a", "member-b", SandboxStatus.Active)]
    [InlineData("member-a", "member-a", SandboxStatus.Active)]
    [InlineData("owner-a", "owner-a", SandboxStatus.Active)]
    [InlineData("owner-a", "missing", SandboxStatus.Active)]
    [InlineData("owner-a", "member-a", SandboxStatus.Passive)]
    [InlineData("owner-a", "member-a", SandboxStatus.Suspended)]
    public async Task Assignment_requires_active_persisted_owner_and_same_sandbox_regular_member(string actor, string target, SandboxStatus status)
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        await db.SetStatusAsync(status);
        var before = await db.SnapshotAsync();
        var roles = new Mock<IRoleStoreFactory>(MockBehavior.Strict);
        var assignments = new Mock<IUserRoleStoreFactory>(MockBehavior.Strict);
        var service = new LabDemoRoleService(ActorProvider.For(UserKey.FromString(actor)), db, roles.Object, assignments.Object);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SetRoleAsync(UserKey.FromString(target), LabRoles.Manager, true));
        Assert.Equal(before, await db.SnapshotAsync());
        roles.VerifyNoOtherCalls();
        assignments.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Demo_role_id_must_match_fixed_identity_before_any_assignment(bool forged)
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var roleStore = new Mock<IRoleStore>();
        roleStore.Setup(x => x.GetByNameAsync("MANAGER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Role.Create(forged ? LabRoleIds.Admin : LabRoleIds.Manager, TenantKeys.Single,
                "Manager", new HashSet<Permission>(), DateTimeOffset.UnixEpoch));
        var roles = new Mock<IRoleStoreFactory>();
        roles.Setup(x => x.Create(It.IsAny<TenantKey>())).Returns(roleStore.Object);
        var userStore = new Mock<IUserRoleStore>(MockBehavior.Strict);
        if (!forged)
        {
            userStore.Setup(x => x.GetAssignmentsAsync(LabDatabase.MemberA, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<UserRole>());
            userStore.Setup(x => x.AssignAsync(LabDatabase.MemberA, LabRoleIds.Manager,
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        }
        var assignments = new Mock<IUserRoleStoreFactory>();
        assignments.Setup(x => x.Create(It.IsAny<TenantKey>())).Returns(userStore.Object);
        var service = new LabDemoRoleService(ActorProvider.For(LabDatabase.OwnerA), db, roles.Object, assignments.Object);
        if (forged)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetRoleAsync(LabDatabase.MemberA, "Manager", true));
            userStore.VerifyNoOtherCalls();
        }
        else
        {
            await service.SetRoleAsync(LabDatabase.MemberA, "Manager", true);
            userStore.Verify(x => x.AssignAsync(LabDatabase.MemberA, LabRoleIds.Manager,
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
